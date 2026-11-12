using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LukkyBoost.App.Core.Enums;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.ViewModels;

public sealed partial class PcDashboardViewModel : ObservableObject, IDisposable
{
    private const int MaxBufferPoints = 30;

    private readonly IHardwareMonitorService _monitorService;
    private readonly IProfileService _profileService;

    // Búferes deslizantes de 30 muestras para MicroGraphControl
    public ObservableCollection<double> CpuHistory { get; } = [];
    public ObservableCollection<double> GpuHistory { get; } = [];
    public ObservableCollection<double> RamHistory { get; } = [];
    public ObservableCollection<double> StorageHistory { get; } = [];

    // Estado de los RadioButtons de Perfiles (56px+)
    [ObservableProperty] private bool _isJuegosSelected;
    [ObservableProperty] private bool _isCodigoSelected;
    [ObservableProperty] private bool _isOficinaSelected;
    [ObservableProperty] private bool _isNormalSelected = true;

    // 1. CPU
    [ObservableProperty] private string _cpuModel = "Detectando CPU...";
    [ObservableProperty] private double _cpuClockMhz;
    [ObservableProperty] private double _cpuLoadPercent;
    [ObservableProperty] private double _cpuTempCelsius;
    [ObservableProperty] private string _cpuTempStatus = "Normal";
    [ObservableProperty] private double _cpuMinPercent;
    [ObservableProperty] private double _cpuAvgPercent;
    [ObservableProperty] private double _cpuMaxPercent;

    // 2. GPU
    [ObservableProperty] private string _gpuModel = "Detectando GPU...";
    [ObservableProperty] private string _gpuDriverVersion = "WHQL";
    [ObservableProperty] private double _gpuLoadPercent;
    [ObservableProperty] private double _gpuVramUsedMb;
    [ObservableProperty] private double _gpuVramTotalMb;
    [ObservableProperty] private double _gpuTempCelsius;
    [ObservableProperty] private string _gpuTempStatus = "Normal";
    [ObservableProperty] private double _gpuMinPercent;
    [ObservableProperty] private double _gpuAvgPercent;
    [ObservableProperty] private double _gpuMaxPercent;

    // 3. RAM
    [ObservableProperty] private double _ramUsedGb;
    [ObservableProperty] private double _ramTotalGb;
    [ObservableProperty] private double _ramUsagePercent;
    [ObservableProperty] private int _ramSpeedMhz;
    [ObservableProperty] private double _ramMinPercent;
    [ObservableProperty] private double _ramAvgPercent;
    [ObservableProperty] private double _ramMaxPercent;

    // 4. ALMACENAMIENTO (DISCO)
    [ObservableProperty] private string _storageModel = "NVMe SSD Principal";
    [ObservableProperty] private double _storageTransferMbPerSec;
    [ObservableProperty] private double _storageFreeSpaceGb;
    [ObservableProperty] private double _storageTotalSpaceGb;
    [ObservableProperty] private double _storageMinMbPerSec;
    [ObservableProperty] private double _storageAvgMbPerSec;
    [ObservableProperty] private double _storageMaxMbPerSec;

    private bool _hasRealSamples;

    public PcDashboardViewModel(
        IHardwareMonitorService monitorService,
        IProfileService profileService)
    {
        _monitorService = monitorService;
        _profileService = profileService;

        for (int i = 0; i < MaxBufferPoints; i++)
        {
            CpuHistory.Add(0);
            GpuHistory.Add(0);
            RamHistory.Add(0);
            StorageHistory.Add(0);
        }

        _monitorService.OnTelemetryUpdated += OnHardwareTelemetryUpdated;
    }

    public void SyncProfileSelection(PerformanceProfile profile)
    {
        IsJuegosSelected = profile == PerformanceProfile.Juegos;
        IsCodigoSelected = profile == PerformanceProfile.Programacion;
        IsOficinaSelected = profile == PerformanceProfile.Oficina;
        IsNormalSelected = profile == PerformanceProfile.Normal;
    }

    [RelayCommand]
    private async Task ApplyProfileCardAsync(string? profileKey)
    {
        if (!Enum.TryParse<PerformanceProfile>(profileKey, ignoreCase: true, out var target))
        {
            return;
        }

        SyncProfileSelection(target);
        await _profileService.ApplyProfileAsync(target);
    }

    private void OnHardwareTelemetryUpdated(HardwareSnapshot snapshot)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            ApplyMetrics(snapshot);
        }
        else
        {
            _ = dispatcher.InvokeAsync(() => ApplyMetrics(snapshot));
        }
    }

    public void ApplyMetrics(HardwareSnapshot m)
    {
        // 1. CPU
        CpuModel = m.CpuModel;
        CpuClockMhz = m.CpuClockMhz;
        CpuLoadPercent = m.CpuLoadPercent;
        CpuTempCelsius = m.CpuTempCelsius;
        CpuTempStatus = ResolveThermalStatus(m.CpuTempCelsius);

        // 2. GPU
        GpuModel = m.GpuModel;
        GpuDriverVersion = m.GpuDriverVersion;
        GpuLoadPercent = m.GpuLoadPercent;
        GpuVramUsedMb = m.GpuVramUsedMb;
        GpuVramTotalMb = m.GpuVramTotalMb;
        GpuTempCelsius = m.GpuTempCelsius;
        GpuTempStatus = ResolveThermalStatus(m.GpuTempCelsius);

        // 3. RAM
        RamUsedGb = m.RamUsedGb;
        RamTotalGb = m.RamTotalGb;
        RamUsagePercent = m.RamUsagePercent;
        RamSpeedMhz = m.RamSpeedMhz;

        // 4. DISCO
        StorageModel = m.StorageModel;
        StorageTransferMbPerSec = m.StorageTransferRateMbPerSec;
        StorageFreeSpaceGb = m.StorageFreeSpaceGb;
        StorageTotalSpaceGb = m.StorageTotalSpaceGb;

        if (!_hasRealSamples)
        {
            _hasRealSamples = true;
            SeedInitialBuffer(CpuHistory, m.CpuLoadPercent);
            SeedInitialBuffer(GpuHistory, m.GpuLoadPercent);
            SeedInitialBuffer(RamHistory, m.RamUsagePercent);
            SeedInitialBuffer(StorageHistory, m.StorageTransferRateMbPerSec);
        }
        else
        {
            PushSample(CpuHistory, m.CpuLoadPercent);
            PushSample(GpuHistory, m.GpuLoadPercent);
            PushSample(RamHistory, m.RamUsagePercent);
            PushSample(StorageHistory, m.StorageTransferRateMbPerSec);
        }

        UpdateStats(CpuHistory, out double cpuMin, out double cpuAvg, out double cpuMax);
        CpuMinPercent = cpuMin;
        CpuAvgPercent = cpuAvg;
        CpuMaxPercent = cpuMax;

        UpdateStats(GpuHistory, out double gpuMin, out double gpuAvg, out double gpuMax);
        GpuMinPercent = gpuMin;
        GpuAvgPercent = gpuAvg;
        GpuMaxPercent = gpuMax;

        UpdateStats(RamHistory, out double ramMin, out double ramAvg, out double ramMax);
        RamMinPercent = ramMin;
        RamAvgPercent = ramAvg;
        RamMaxPercent = ramMax;

        UpdateStats(StorageHistory, out double dskMin, out double dskAvg, out double dskMax);
        StorageMinMbPerSec = dskMin;
        StorageAvgMbPerSec = dskAvg;
        StorageMaxMbPerSec = dskMax;
    }

    private static string ResolveThermalStatus(double tempCelsius)
    {
        if (tempCelsius >= 82) return "Alto";
        if (tempCelsius >= 70) return "Templado";
        return "Normal";
    }

    private static void SeedInitialBuffer(ObservableCollection<double> buffer, double initialValue)
    {
        buffer.Clear();
        for (int i = 0; i < MaxBufferPoints; i++)
        {
            buffer.Add(initialValue);
        }
    }

    private static void PushSample(ObservableCollection<double> buffer, double value)
    {
        if (buffer.Count >= MaxBufferPoints)
        {
            buffer.RemoveAt(0);
        }

        buffer.Add(value);
    }

    private static void UpdateStats(
        ObservableCollection<double> buffer,
        out double min,
        out double avg,
        out double max)
    {
        if (buffer.Count == 0)
        {
            min = avg = max = 0;
            return;
        }

        min = Math.Round(buffer.Min(), 0);
        avg = Math.Round(buffer.Average(), 0);
        max = Math.Round(buffer.Max(), 0);
    }

    public void Dispose()
    {
        _monitorService.OnTelemetryUpdated -= OnHardwareTelemetryUpdated;
    }
}
