using System.IO;
using System.Management;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;
using LukkyBoost.App.Core.Win32;
using Microsoft.Win32;

namespace LukkyBoost.App.Services;

public sealed class GpuShaderService : IGpuDriverService
{
    public Task<GpuDriverInfo> DetectActiveGpuAndShadersAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            string gpuName = "GPU Principal";
            string rawWmiVersion = "0.0.0.0";

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, DriverVersion FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string candidate = obj["Name"]?.ToString()?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(candidate)) continue;

                    gpuName = candidate;
                    rawWmiVersion = obj["DriverVersion"]?.ToString()?.Trim() ?? rawWmiVersion;

                    if (candidate.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                        candidate.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                        candidate.Contains("RTX", StringComparison.OrdinalIgnoreCase) ||
                        candidate.Contains("RX ", StringComparison.OrdinalIgnoreCase) ||
                        candidate.Contains("Arc", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }
            }
            catch { }

            string vendor = ResolveVendor(gpuName);
            string formattedDriver = FormatPublicDriverVersion(vendor, rawWmiVersion);
            string officialUrl = vendor switch
            {
                "NVIDIA" => "https://www.nvidia.com/Download/index.aspx",
                "AMD"    => "https://www.amd.com/en/support/download/drivers.html",
                "Intel"  => "https://www.intel.com/content/www/us/en/download-center/home.html",
                _        => "https://www.guru3d.com/files-details/display-driver-uninstaller-download.html"
            };

            double nvidiaSizeMb = CalculateDirectoriesSizeMb(GetNvidiaShaderPaths());
            double amdAndD3DSizeMb = CalculateDirectoriesSizeMb(GetAmdAndDirectXShaderPaths());
            bool wuBlocked = IsWindowsUpdateDriverBlocked();

            return new GpuDriverInfo(
                Vendor: vendor,
                ModelName: gpuName,
                InstalledDriverVersion: formattedDriver,
                WmiRawVersion: rawWmiVersion,
                OfficialDriverUrl: officialUrl,
                NvidiaShaderCacheSizeMb: nvidiaSizeMb,
                AmdAndD3DShaderCacheSizeMb: amdAndD3DSizeMb,
                WindowsUpdateDriverBlocked: wuBlocked);
        }, cancellationToken);
    }

    public Task<ShaderPurgeResult> PurgeShaderCacheAsync(
        ShaderCacheTarget target,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var directories = target switch
            {
                ShaderCacheTarget.NvidiaDxAndGl => GetNvidiaShaderPaths(),
                ShaderCacheTarget.AmdAndD3D     => GetAmdAndDirectXShaderPaths(),
                ShaderCacheTarget.AllCaches     => [.. GetNvidiaShaderPaths(), .. GetAmdAndDirectXShaderPaths()],
                _                               => [.. GetNvidiaShaderPaths(), .. GetAmdAndDirectXShaderPaths()]
            };

            long bytesFreed = 0;
            int filesDeleted = 0;
            int lockedSkipped = 0;

            var enumOptions = new System.IO.EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            foreach (string dir in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Directory.Exists(dir)) continue;

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(dir, "*", enumOptions);
                }
                catch
                {
                    continue;
                }

                foreach (string file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var info = new FileInfo(file);
                        long fileSize = info.Exists ? info.Length : 0;
                        if (info.IsReadOnly) info.IsReadOnly = false;

                        File.Delete(file);
                        bytesFreed += fileSize;
                        filesDeleted++;
                    }
                    catch (IOException)
                    {
                        // Archivos en uso por DWM o procesos 3D se ignoran sin bloquear
                        lockedSkipped++;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        lockedSkipped++;
                    }
                    catch
                    {
                        lockedSkipped++;
                    }
                }
            }

            double freedMb = Math.Round(bytesFreed / (1024.0 * 1024.0), 2);
            return new ShaderPurgeResult(target, freedMb, filesDeleted, lockedSkipped);
        }, cancellationToken);
    }

    public Task<bool> RestartVideoSubsystemHotAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                return NativeMethods.SendWinCtrlShiftB();
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    public Task<bool> SetWindowsUpdateDriverBlockAsync(bool block, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching",
                    writable: true);
                key?.SetValue("SearchOrderConfig", block ? 0 : 1, RegistryValueKind.DWord);
                return true;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    private static IReadOnlyList<string> GetNvidiaShaderPaths()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        return
        [
            Path.Combine(local, "NVIDIA", "DXCache"),
            Path.Combine(local, "NVIDIA", "GLCache"),
            Path.Combine(roaming, "NVIDIA", "ComputeCache"),
            Path.Combine(local, "NVIDIA Corporation", "NV_Cache"),
            Path.Combine(programData, "NVIDIA Corporation", "NV_Cache")
        ];
    }

    private static IReadOnlyList<string> GetAmdAndDirectXShaderPaths()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return
        [
            Path.Combine(local, "AMD", "DxCache"),
            Path.Combine(local, "AMD", "D3DSCache"),
            Path.Combine(local, "AMD", "GLCache"),
            Path.Combine(local, "AMD", "VkCache"),
            Path.Combine(local, "D3DSCache")
        ];
    }

    private static double CalculateDirectoriesSizeMb(IReadOnlyList<string> paths)
    {
        long totalBytes = 0;
        var enumOptions = new System.IO.EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true
        };

        foreach (string dir in paths)
        {
            if (!Directory.Exists(dir)) continue;

            try
            {
                foreach (string file in Directory.EnumerateFiles(dir, "*", enumOptions))
                {
                    try
                    {
                        totalBytes += new FileInfo(file).Length;
                    }
                    catch { }
                }
            }
            catch { }
        }

        return Math.Round(totalBytes / (1024.0 * 1024.0), 2);
    }

    private static bool IsWindowsUpdateDriverBlocked()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching");
            return (key?.GetValue("SearchOrderConfig") as int?) == 0;
        }
        catch { return false; }
    }

    private static string ResolveVendor(string gpuName)
    {
        if (gpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
            gpuName.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
            gpuName.Contains("RTX", StringComparison.OrdinalIgnoreCase) ||
            gpuName.Contains("GTX", StringComparison.OrdinalIgnoreCase))
        {
            return "NVIDIA";
        }

        if (gpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            gpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
        {
            return "AMD";
        }

        if (gpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
            gpuName.Contains("Arc", StringComparison.OrdinalIgnoreCase) ||
            gpuName.Contains("Iris", StringComparison.OrdinalIgnoreCase))
        {
            return "Intel";
        }

        return "GPU";
    }

    private static string FormatPublicDriverVersion(string vendor, string rawWmiVersion)
    {
        if (vendor == "NVIDIA")
        {
            string digits = new string(rawWmiVersion.Where(char.IsDigit).ToArray());
            if (digits.Length >= 5)
            {
                string tail = digits[^5..];
                return $"{tail[..3]}.{tail[3..]} WHQL";
            }
        }

        return rawWmiVersion;
    }
}

