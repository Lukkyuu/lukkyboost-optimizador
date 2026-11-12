using System.Diagnostics;
using System.IO;
using System.Text.Json;
using LukkyBoost.App.Core.Enums;
using LukkyBoost.App.Core.Interfaces;

namespace LukkyBoost.App.Services;

public sealed class ProfileService : IProfileService
{
    private const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    private const string HighPerformanceGuid     = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string BalancedGuid            = "381b4222-f694-41f0-9685-ff5bb260df2e";
    private const string PowerSaverGuid          = "a1841308-3541-4fab-bc81-f71556f20b4a";

    private readonly ISystemOptimizerService _optimizerService;
    private readonly string _profileStatePath;

    public PerformanceProfile CurrentProfile { get; private set; } = PerformanceProfile.Normal;

    public ProfileService(ISystemOptimizerService optimizerService)
    {
        _optimizerService = optimizerService;
        string appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "lukkyboost");
        Directory.CreateDirectory(appFolder);
        _profileStatePath = Path.Combine(appFolder, "last_profile.json");
    }

    public async Task<PerformanceProfile> LoadLastSavedProfileAsync()
    {
        try
        {
            if (File.Exists(_profileStatePath))
            {
                string json = await File.ReadAllTextAsync(_profileStatePath).ConfigureAwait(false);
                if (Enum.TryParse<PerformanceProfile>(json.Trim('"'), ignoreCase: true, out var saved))
                {
                    CurrentProfile = saved;
                }
            }
        }
        catch
        {
            CurrentProfile = PerformanceProfile.Normal;
        }

        return CurrentProfile;
    }

    public async Task<string> ApplyProfileAsync(PerformanceProfile profile, CancellationToken cancellationToken = default)
    {
        CurrentProfile = profile;
        await SaveProfileStateAsync(profile).ConfigureAwait(false);

        switch (profile)
        {
            case PerformanceProfile.Juegos:
                await EnableUltimatePerformanceAsync(cancellationToken).ConfigureAwait(false);
                await _optimizerService.SetIpv4PreferredAndTcpNoDelayAsync(true, cancellationToken).ConfigureAwait(false);
                await _optimizerService.SetWindowsTelemetryDisabledAsync(true, cancellationToken).ConfigureAwait(false);
                await _optimizerService.SetServicesToManualModeAsync(true, cancellationToken).ConfigureAwait(false);
                return "Perfil Juegos activo";

            case PerformanceProfile.Programacion:
                await SetPowerPlanAsync(HighPerformanceGuid, cancellationToken).ConfigureAwait(false);
                await EnsureDevServicesReadyAsync(cancellationToken).ConfigureAwait(false);
                return "Perfil Código activo";

            case PerformanceProfile.Oficina:
                await SetPowerPlanAsync(PowerSaverGuid, cancellationToken).ConfigureAwait(false);
                await _optimizerService.SetWindowsTelemetryDisabledAsync(true, cancellationToken).ConfigureAwait(false);
                return "Perfil Oficina activo";

            case PerformanceProfile.Normal:
            default:
                await SetPowerPlanAsync(BalancedGuid, cancellationToken).ConfigureAwait(false);
                await _optimizerService.SetIpv4PreferredAndTcpNoDelayAsync(false, cancellationToken).ConfigureAwait(false);
                await _optimizerService.SetWindowsTelemetryDisabledAsync(false, cancellationToken).ConfigureAwait(false);
                return "Perfil Normal activo";
        }
    }

    private async Task SaveProfileStateAsync(PerformanceProfile profile)
    {
        try
        {
            string json = JsonSerializer.Serialize(profile.ToString());
            await File.WriteAllTextAsync(_profileStatePath, json).ConfigureAwait(false);
        }
        catch { }
    }

    private static async Task EnableUltimatePerformanceAsync(CancellationToken ct)
    {
        await RunPowerCfgAsync($"-duplicatescheme {UltimatePerformanceGuid}", ct).ConfigureAwait(false);
        await SetPowerPlanAsync(UltimatePerformanceGuid, ct).ConfigureAwait(false);
        await SetPowerPlanAsync(HighPerformanceGuid, ct).ConfigureAwait(false);
    }

    private static Task SetPowerPlanAsync(string schemeGuid, CancellationToken ct) =>
        RunPowerCfgAsync($"/setactive {schemeGuid}", ct);

    private static async Task RunPowerCfgAsync(string arguments, CancellationToken ct)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc is not null) await proc.WaitForExitAsync(ct).ConfigureAwait(false);
        }
        catch { }
    }

    private static async Task EnsureDevServicesReadyAsync(CancellationToken ct)
    {
        string[] devServices = ["WslService", "vmcompute", "com.docker.service", "MySQL80", "postgresql-x64-16"];
        foreach (string svc in devServices)
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"config \"{svc}\" start= demand",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (proc is not null) await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            }
            catch { }
        }
    }
}
