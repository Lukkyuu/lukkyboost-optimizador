using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;
using LukkyBoost.App.Core.Win32;

namespace LukkyBoost.App.Services;

public sealed class HardwareMonitorService : IHardwareMonitorService
{
    private readonly Computer _computer;
    private readonly SensorUpdateVisitor _visitor = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    private CancellationTokenSource? _pollingCts;
    private bool _isInitialized;
    private bool _disposed;

    // Caché estática WMI
    private string _cpuModel = "Procesador x64";
    private double _cpuBaseMhz = 3600;
    private string _gpuModel = "GPU Principal";
    private string _gpuVendor = "NVIDIA";
    private string _gpuDriverVersion = "N/A";
    private double _gpuStaticVramMb = 8192;
    private int _ramSpeedMhz = 3200;
    private string _storageModel = "NVMe SSD Principal";

    public event EventHandler<TelemetryMetrics>? MetricsUpdated;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = true
        };
    }

    public async Task InitializeSensorsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isInitialized) return;

            await Task.Run(() =>
            {
                try
                {
                    _computer.Open();
                }
                catch
                {
                    // Fallback silencioso si el driver Ring0 está restringido
                }

                LoadStaticWmiInventory();
                _isInitialized = true;
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<TelemetryMetrics> CaptureSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            await InitializeSensorsAsync(cancellationToken).ConfigureAwait(false);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                try
                {
                    _computer.Accept(_visitor);
                }
                catch
                {
                    // Ignorar fallos transitorios de bus SMBus/PCIe
                }

                // 1. CPU
                var cpuHw = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
                double cpuClock = _cpuBaseMhz;
                double cpuLoad = 0;
                double cpuTemp = 45;

                if (cpuHw is not null)
                {
                    var clocks = cpuHw.Sensors
                        .Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) && s.Value.HasValue)
                        .Select(s => (double)s.Value!.Value)
                        .ToList();
                    if (clocks.Count > 0) cpuClock = Math.Round(clocks.Average(), 0);

                    cpuLoad = Math.Round(FindSensor(cpuHw, SensorType.Load, "Total") ?? 0, 1);
                    cpuTemp = Math.Round(
                        FindSensor(cpuHw, SensorType.Temperature, "Package")
                        ?? FindSensor(cpuHw, SensorType.Temperature, "Tctl/Tdie")
                        ?? FindSensor(cpuHw, SensorType.Temperature, "Core")
                        ?? 45, 1);
                }

                // 2. GPU
                var gpuHw = _computer.Hardware.FirstOrDefault(h =>
                    h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel);

                double gpuLoad = 0;
                double gpuVramUsed = 0;
                double gpuVramTotal = _gpuStaticVramMb;
                double gpuTemp = 40;

                if (gpuHw is not null)
                {
                    _gpuModel = gpuHw.Name;
                    _gpuVendor = gpuHw.HardwareType switch
                    {
                        HardwareType.GpuNvidia => "NVIDIA",
                        HardwareType.GpuAmd => "AMD",
                        HardwareType.GpuIntel => "Intel",
                        _ => _gpuVendor
                    };

                    gpuLoad = Math.Round(
                        FindSensor(gpuHw, SensorType.Load, "GPU Core")
                        ?? FindSensor(gpuHw, SensorType.Load, "D3D 3D")
                        ?? 0, 1);

                    gpuVramUsed = Math.Round(
                        FindSensor(gpuHw, SensorType.SmallData, "GPU Memory Used")
                        ?? FindSensor(gpuHw, SensorType.SmallData, "D3D Dedicated Memory Used")
                        ?? 0, 0);

                    gpuVramTotal = Math.Round(
                        FindSensor(gpuHw, SensorType.SmallData, "GPU Memory Total")
                        ?? FindSensor(gpuHw, SensorType.SmallData, "D3D Dedicated Memory Total")
                        ?? _gpuStaticVramMb, 0);

                    gpuTemp = Math.Round(
                        FindSensor(gpuHw, SensorType.Temperature, "GPU Core")
                        ?? FindSensor(gpuHw, SensorType.Temperature, "Hot Spot")
                        ?? 40, 1);
                }

                // 3. RAM
                double ramTotalGb = 16;
                double ramUsedGb = 6;
                double ramPercent = 37;

                var memStatus = new NativeMethods.MEMORYSTATUSEX
                {
                    dwLength = (uint)Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>()
                };
                if (NativeMethods.GlobalMemoryStatusEx(ref memStatus))
                {
                    ramTotalGb = Math.Round(memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0), 1);
                    double availGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                    ramUsedGb = Math.Round(ramTotalGb - availGb, 1);
                    ramPercent = memStatus.dwMemoryLoad;
                }

                // 4. Almacenamiento (Tasa de transferencia MB/s y espacio libre)
                var storageHw = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Storage);
                double transferMbPerSec = 0;
                if (storageHw is not null)
                {
                    _storageModel = storageHw.Name;
                    // SensorType.Throughput entrega Bytes/s en LibreHardwareMonitor
                    double readBytesSec = FindSensor(storageHw, SensorType.Throughput, "Read") ?? 0;
                    double writeBytesSec = FindSensor(storageHw, SensorType.Throughput, "Write") ?? 0;
                    transferMbPerSec = Math.Round((readBytesSec + writeBytesSec) / (1024.0 * 1024.0), 2);
                }

                double freeDiskGb = 0;
                double totalDiskGb = 0;
                try
                {
                    string sysRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                    var driveInfo = new DriveInfo(sysRoot);
                    if (driveInfo.IsReady)
                    {
                        freeDiskGb = Math.Round(driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 1);
                        totalDiskGb = Math.Round(driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
                    }
                }
                catch
                {
                    // Ignorar errores de acceso a volumen
                }

                return new TelemetryMetrics(
                    CpuModel: _cpuModel,
                    CpuClockMhz: cpuClock,
                    CpuLoadPercent: cpuLoad,
                    CpuTempCelsius: cpuTemp,
                    GpuModel: _gpuModel,
                    GpuVendor: _gpuVendor,
                    GpuDriverVersion: _gpuDriverVersion,
                    GpuLoadPercent: gpuLoad,
                    GpuVramUsedMb: gpuVramUsed,
                    GpuVramTotalMb: gpuVramTotal,
                    GpuTempCelsius: gpuTemp,
                    RamUsedGb: ramUsedGb,
                    RamTotalGb: ramTotalGb,
                    RamUsagePercent: ramPercent,
                    RamSpeedMhz: _ramSpeedMhz,
                    StorageModel: _storageModel,
                    StorageTransferRateMbPerSec: transferMbPerSec,
                    StorageFreeSpaceGb: freeDiskGb,
                    StorageTotalSpaceGb: totalDiskGb);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void StartPolling(TimeSpan interval)
    {
        StopPolling();
        _pollingCts = new CancellationTokenSource();
        var token = _pollingCts.Token;

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(interval);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var metrics = await CaptureSnapshotAsync(token).ConfigureAwait(false);
                    MetricsUpdated?.Invoke(this, metrics);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Silencioso para mantener estabilidad 24/7
                }

                if (!await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                {
                    break;
                }
            }
        }, token);
    }

    public void StopPolling()
    {
        if (_pollingCts is not null)
        {
            _pollingCts.Cancel();
            _pollingCts.Dispose();
            _pollingCts = null;
        }
    }

    private void LoadStaticWmiInventory()
    {
        try
        {
            using var cpuSearcher = new ManagementObjectSearcher("SELECT Name, MaxClockSpeed FROM Win32_Processor");
            foreach (ManagementObject obj in cpuSearcher.Get())
            {
                _cpuModel = obj["Name"]?.ToString()?.Trim() ?? _cpuModel;
                _cpuBaseMhz = Convert.ToDouble(obj["MaxClockSpeed"] ?? _cpuBaseMhz);
                break;
            }
        }
        catch { }

        try
        {
            using var gpuSearcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController");
            foreach (ManagementObject obj in gpuSearcher.Get())
            {
                _gpuModel = obj["Name"]?.ToString()?.Trim() ?? _gpuModel;
                _gpuDriverVersion = obj["DriverVersion"]?.ToString()?.Trim() ?? _gpuDriverVersion;
                ulong ramBytes = Convert.ToUInt64(obj["AdapterRAM"] ?? 0UL);
                if (ramBytes > 0)
                {
                    _gpuStaticVramMb = Math.Round(ramBytes / (1024.0 * 1024.0), 0);
                }
                break;
            }
        }
        catch { }

        try
        {
            using var memSearcher = new ManagementObjectSearcher("SELECT ConfiguredClockSpeed, Speed FROM Win32_PhysicalMemory");
            foreach (ManagementObject obj in memSearcher.Get())
            {
                int speed = Convert.ToInt32(obj["ConfiguredClockSpeed"] ?? obj["Speed"] ?? 0);
                if (speed > _ramSpeedMhz) _ramSpeedMhz = speed;
            }
        }
        catch { }
    }

    private static double? FindSensor(IHardware hw, SensorType type, string partialName)
    {
        var sensor = hw.Sensors.FirstOrDefault(s =>
            s.SensorType == type &&
            s.Name.Contains(partialName, StringComparison.OrdinalIgnoreCase) &&
            s.Value.HasValue);
        return sensor?.Value is float v ? v : null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopPolling();
        _gate.Dispose();
        try { _computer.Close(); } catch { }
        _disposed = true;
    }

    private sealed class SensorUpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware) sub.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}

