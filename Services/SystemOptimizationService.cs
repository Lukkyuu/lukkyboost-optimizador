using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using LukkyBoost.App.Core.Interfaces;
using Microsoft.Win32;

namespace LukkyBoost.App.Services;

public sealed class SystemOptimizationService : ISystemOptimizerService
{
    public Task<bool> ApplyTweakByIdAsync(string tweakId, bool enable, CancellationToken ct = default)
    {
        return tweakId switch
        {
            // A) Debloat
            "debloat.windows_ai"        => SetWindowsAiDisabledAsync(enable, ct),
            "debloat.ms_apps"           => RemoveMicrosoftBloatwareAppsAsync(enable, ct),
            "debloat.third_party"       => RemoveThirdPartyPromotedAppsAsync(enable, ct),
            "debloat.extensions"        => RemoveOemCodecExtensionsAsync(enable, ct),
            "debloat.xbox"              => RemoveXboxComponentsAsync(enable, ct),
            "debloat.ms_store"          => SetMicrosoftStoreDisabledAsync(enable, ct),
            "debloat.store_autoupdate"  => SetStoreAutoUpdatesDisabledAsync(enable, ct),
            "debloat.onedrive"          => UninstallOneDriveAsync(enable, ct),
            "debloat.edge"              => SetEdgeBloatReducedAsync(enable, ct),
            "debloat.brave"             => SetBraveBloatReducedAsync(enable, ct),
            "debloat.widgets"           => RemoveWindowsWidgetsAsync(enable, ct),

            // B) Privacidad
            "privacy.app_permissions"   => SetGlobalAppPermissionsDisabledAsync(enable, ct),
            "privacy.wu_pause_20y"      => SetWindowsUpdatePauseLimit20YearsAsync(enable, ct),
            "privacy.wpbt"              => SetWpbtDisabledAsync(enable, ct),
            "privacy.bitlocker_auto"    => SetBitLockerAutoEncryptionDisabledAsync(enable, ct),
            "privacy.cloud_sync"        => SetCloudSyncDisabledAsync(enable, ct),
            "privacy.timeline"          => SetActivityHistoryTimelineDisabledAsync(enable, ct),
            "privacy.notifications"     => SetNotificationCenterDisabledAsync(enable, ct),
            "privacy.offline_maps"      => SetOfflineMapsAutoDownloadDisabledAsync(enable, ct),
            "privacy.default0_user"     => RemoveDefault0OobeUserAsync(enable, ct),
            "privacy.lockscreen_cam"    => SetLockScreenCamAndBiometricsDisabledAsync(enable, ct),

            // C) Telemetría
            "telemetry.windows_core"    => SetWindowsTelemetryDisabledAsync(enable, ct),
            "telemetry.third_party"     => SetThirdPartyTelemetryDisabledAsync(enable, ct),
            "telemetry.windows_update"  => SetWindowsUpdateDeliveryOptDisabledAsync(enable, ct),

            // D) Rendimiento
            "perf.manual_services"      => SetServicesToManualModeAsync(enable, ct),
            "perf.disable_transparency" => SetWindowsTransparencyDisabledAsync(enable, ct),
            "perf.disable_hags"         => SetHagsDisabledAsync(enable, ct),
            "perf.ipv4_tcp_nagle"       => SetIpv4PreferredAndTcpNoDelayAsync(enable, ct),
            "perf.menu_show_delay"      => SetMouseMenuDelayZeroAsync(enable, ct),
            "perf.defender_cpu_limit"   => SetDefenderCpuLimit25PercentAsync(enable, ct),
            "perf.disable_vbs"          => SetCoreIsolationVbsDisabledAsync(enable, ct),
            "perf.disable_sysmain"      => SetSysMainDisabledAsync(enable, ct),
            "perf.disable_search"       => SetStorageSenseAndSearchDisabledAsync(enable, ct),
            "perf.disable_fast_startup" => SetFastStartupDisabledAsync(enable, ct),

            // E) Varios
            "misc.classic_context_menu" => SetWin10ClassicContextMenuAsync(enable, ct),
            "misc.disable_mpo"          => SetMpoDisabledAsync(enable, ct),
            "misc.taskbar_end_task"     => SetTaskbarEndTaskEnabledAsync(enable, ct),
            "misc.remove_home_gallery"  => SetExplorerHomeAndGalleryRemovedAsync(enable, ct),
            "misc.taskbar_left"         => SetTaskbarIconsLeftAsync(enable, ct),
            "misc.numlock_stickykeys"   => SetNumLockAndStickyKeysDisabledAsync(enable, ct),
            "misc.force_dark_mode"      => SetSystemDarkModeForcedAsync(enable, ct),
            "misc.show_extensions"      => SetShowFileExtensionsAndHiddenAsync(enable, ct),
            "misc.disable_snap_layouts" => SetSnapLayoutsDisabledAsync(enable, ct),
            "misc.detailed_bsod"        => SetDetailedBsodAndVerboseLogonAsync(enable, ct),

            _ => Task.FromResult(false)
        };
    }

    // =========================================================================
    // A) DEBLOAT
    // =========================================================================

    public Task<bool> SetWindowsAiDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", disable ? 1 : null);
                SetDword(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", disable ? 1 : null);
                SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", disable ? 0 : 1);
                SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", disable ? 1 : null);
                SetDword(Registry.CurrentUser, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", disable ? 1 : null);
                return true;
            }
            catch { return false; }
        }, ct);

    public async Task<bool> RemoveMicrosoftBloatwareAppsAsync(bool remove, CancellationToken ct = default)
    {
        if (!remove) return true;
        string[] patterns = ["*MicrosoftSolitaireCollection*", "*People*", "*WindowsMaps*", "*WindowsFeedbackHub*", "*ZuneMusic*", "*ZuneVideo*", "*BingNews*", "*BingWeather*"];
        return await RemoveAppxPackagesAsync(patterns, ct).ConfigureAwait(false);
    }

    public async Task<bool> RemoveThirdPartyPromotedAppsAsync(bool remove, CancellationToken ct = default)
    {
        SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SilentInstalledAppsEnabled", remove ? 0 : 1);
        SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "OemPreInstalledAppsEnabled", remove ? 0 : 1);
        SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "PreInstalledAppsEnabled", remove ? 0 : 1);

        if (!remove) return true;
        string[] patterns = ["*Spotify*", "*Shazam*", "*Clipchamp*", "*Disney*", "*TikTok*", "*Instagram*"];
        return await RemoveAppxPackagesAsync(patterns, ct).ConfigureAwait(false);
    }

    public async Task<bool> RemoveOemCodecExtensionsAsync(bool remove, CancellationToken ct = default)
    {
        if (!remove) return true;
        string[] patterns = ["*HEVCVideoExtension*", "*VP9VideoExtensions*", "*WebMediaExtensions*"];
        return await RemoveAppxPackagesAsync(patterns, ct).ConfigureAwait(false);
    }

    public async Task<bool> RemoveXboxComponentsAsync(bool remove, CancellationToken ct = default)
    {
        string[] xboxServices = ["XblAuthManager", "XblGameSave", "XboxNetApiSvc", "XboxGipSvc"];
        foreach (string svc in xboxServices)
        {
            await ConfigureServiceAsync(svc, remove ? "disabled" : "demand", stopNow: remove, ct).ConfigureAwait(false);
        }

        if (remove)
        {
            string[] patterns = ["*XboxApp*", "*XboxGamingOverlay*", "*XboxGameOverlay*", "*XboxSpeechToTextOverlay*"];
            await RemoveAppxPackagesAsync(patterns, ct).ConfigureAwait(false);
        }

        return true;
    }

    public Task<bool> SetMicrosoftStoreDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\WindowsStore", "RemoveWindowsStore", disable ? 1 : null);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetStoreAutoUpdatesDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                // AutoDownload: 2 = Nunca descargar automáticamente, 4 = Descargar automáticamente
                SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", disable ? 2 : 4);
                return true;
            }
            catch { return false; }
        }, ct);

    public async Task<bool> UninstallOneDriveAsync(bool uninstall, CancellationToken ct = default)
    {
        try
        {
            if (uninstall)
            {
                await RunHiddenProcessAsync("taskkill.exe", "/F /IM OneDrive.exe", ct).ConfigureAwait(false);
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string setup64 = Path.Combine(winDir, "SysWOW64", "OneDriveSetup.exe");
                string setup32 = Path.Combine(winDir, "System32", "OneDriveSetup.exe");

                if (File.Exists(setup64))
                    await RunHiddenProcessAsync(setup64, "/uninstall", ct).ConfigureAwait(false);
                else if (File.Exists(setup32))
                    await RunHiddenProcessAsync(setup32, "/uninstall", ct).ConfigureAwait(false);
            }

            int pinVal = uninstall ? 0 : 1;
            SetDword(Registry.ClassesRoot, @"CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}", "System.IsPinnedToNameSpaceTree", pinVal);
            SetDword(Registry.ClassesRoot, @"Wow6432Node\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}", "System.IsPinnedToNameSpaceTree", pinVal);
            return true;
        }
        catch { return false; }
    }

    public Task<bool> SetEdgeBloatReducedAsync(bool reduce, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                const string edgePol = @"SOFTWARE\Policies\Microsoft\Edge";
                SetDword(Registry.LocalMachine, edgePol, "HubsSidebarEnabled", reduce ? 0 : null);
                SetDword(Registry.LocalMachine, edgePol, "EdgeShoppingAssistantEnabled", reduce ? 0 : null);
                SetDword(Registry.LocalMachine, edgePol, "StartupBoostEnabled", reduce ? 0 : null);
                SetDword(Registry.LocalMachine, edgePol, "BackgroundModeEnabled", reduce ? 0 : null);
                SetDword(Registry.LocalMachine, edgePol, "DiagnosticData", reduce ? 0 : null);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetBraveBloatReducedAsync(bool reduce, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                const string bravePol = @"SOFTWARE\Policies\BraveSoftware\Brave";
                SetDword(Registry.LocalMachine, bravePol, "BraveRewardsDisabled", reduce ? 1 : null);
                SetDword(Registry.LocalMachine, bravePol, "BraveWalletDisabled", reduce ? 1 : null);
                SetDword(Registry.LocalMachine, bravePol, "BraveVPNDisabled", reduce ? 1 : null);
                SetDword(Registry.LocalMachine, bravePol, "BraveAIChatEnabled", reduce ? 0 : null);
                return true;
            }
            catch { return false; }
        }, ct);

    public async Task<bool> RemoveWindowsWidgetsAsync(bool remove, CancellationToken ct = default)
    {
        SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", remove ? 0 : null);
        SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", remove ? 0 : 1);

        if (remove)
        {
            await RemoveAppxPackagesAsync(["*WebExperience*"], ct).ConfigureAwait(false);
        }

        return true;
    }

    // =========================================================================
    // B) PRIVACIDAD
    // =========================================================================

    public Task<bool> SetGlobalAppPermissionsDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                string[] capabilities = ["location", "webcam", "microphone", "contacts", "documentsLibrary", "Appointments", "phoneCall"];
                string value = disable ? "Deny" : "Allow";

                foreach (string cap in capabilities)
                {
                    using var key = Registry.LocalMachine.CreateSubKey(
                        $@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{cap}", writable: true);
                    key?.SetValue("Value", value, RegistryValueKind.String);
                }

                SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", disable ? 1 : 0);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetWindowsUpdatePauseLimit20YearsAsync(bool enable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                // 20 años = 7300 días
                SetDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "FlightSettingsMaxPauseDays", enable ? 7300 : 35);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetWpbtDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager", "DisableWpbtExecution", disable ? 1 : 0);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetBitLockerAutoEncryptionDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\BitLocker", "PreventDeviceEncryption", disable ? 1 : 0);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetCloudSyncDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableSettingSync", disable ? 2 : null);
                SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableSettingSyncUserOverride", disable ? 1 : null);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetActivityHistoryTimelineDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                const string sysPol = @"SOFTWARE\Policies\Microsoft\Windows\System";
                SetDword(Registry.LocalMachine, sysPol, "EnableActivityFeed", disable ? 0 : null);
                SetDword(Registry.LocalMachine, sysPol, "PublishUserActivities", disable ? 0 : null);
                SetDword(Registry.LocalMachine, sysPol, "UploadUserActivities", disable ? 0 : null);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetNotificationCenterDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                SetDword(Registry.CurrentUser, @"SOFTWARE\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter", disable ? 1 : 0);
                return true;
            }
            catch { return false; }
        }, ct);

    public async Task<bool> SetOfflineMapsAutoDownloadDisabledAsync(bool disable, CancellationToken ct = default)
    {
        SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Maps", "AutoDownloadAndUpdateMapData", disable ? 0 : null);
        SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Maps", "AllowUntriggeredNetworkTrafficOnSettingsPage", disable ? 0 : null);
        return await ConfigureServiceAsync("MapsBroker", disable ? "disabled" : "demand", stopNow: disable, ct).ConfigureAwait(false);
    }

    public async Task<bool> RemoveDefault0OobeUserAsync(bool remove, CancellationToken ct = default)
    {
        if (!remove) return true;
        await RunHiddenProcessAsync("net.exe", "user defaultuser0 /delete", ct).ConfigureAwait(false);
        await RunHiddenProcessAsync("net.exe", "user default0 /delete", ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetLockScreenCamAndBiometricsDisabledAsync(bool disable, CancellationToken ct = default)
    {
        SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Personalization", "NoLockScreenCamera", disable ? 1 : null);
        SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Biometrics", "Enabled", disable ? 0 : null);
        return await ConfigureServiceAsync("WbioSrvc", disable ? "disabled" : "demand", stopNow: disable, ct).ConfigureAwait(false);
    }

    // =========================================================================
    // C) TELEMETRÍA
    // =========================================================================

    public async Task<bool> SetWindowsTelemetryDisabledAsync(bool disable, CancellationToken ct = default)
    {
        SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", disable ? 0 : null);
        string[] services = ["DiagTrack", "dmwappushservice", "WerSvc"];
        foreach (string svc in services)
        {
            await ConfigureServiceAsync(svc, disable ? "disabled" : "demand", stopNow: disable, ct).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<bool> SetThirdPartyTelemetryDisabledAsync(bool disable, CancellationToken ct = default)
    {
        SetDword(Registry.CurrentUser, @"Software\Policies\Microsoft\office\16.0\osm", "enablelogging", disable ? 0 : null);
        SetDword(Registry.CurrentUser, @"Software\Policies\Microsoft\office\16.0\osm", "enableupload", disable ? 0 : null);
        await ConfigureServiceAsync("NvTelemetryContainer", disable ? "disabled" : "demand", stopNow: disable, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetWindowsUpdateDeliveryOptDisabledAsync(bool disable, CancellationToken ct = default)
    {
        SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", disable ? 0 : null);
        await ConfigureServiceAsync("DoSvc", disable ? "disabled" : "demand", stopNow: disable, ct).ConfigureAwait(false);
        return true;
    }

    // =========================================================================
    // D) RENDIMIENTO
    // =========================================================================

    public async Task<bool> SetServicesToManualModeAsync(bool enable, CancellationToken ct = default)
    {
        string[] services = ["Fax", "PrintNotify", "RemoteRegistry", "wisvc", "icssvc", "PhoneSvc", "RetailDemo", "WalletService"];
        string mode = enable ? "demand" : "auto";
        foreach (string svc in services)
        {
            await ConfigureServiceAsync(svc, mode, stopNow: false, ct).ConfigureAwait(false);
        }

        return true;
    }

    public Task<bool> SetWindowsTransparencyDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", disable ? 0 : 1);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetHagsDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                // HwSchMode: 1 = Desactivado, 2 = Activado
                SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", disable ? 1 : 2);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetIpv4PreferredAndTcpNoDelayAsync(bool enable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                // 0x20 (32) prefiere IPv4 sobre IPv6 en Windows
                SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents", enable ? 0x20 : 0);
                SetDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", enable ? unchecked((int)0xFFFFFFFF) : 10);
                SetDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", enable ? 0 : 20);

                var activeNics = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                n.NetworkInterfaceType is not NetworkInterfaceType.Loopback and not NetworkInterfaceType.Tunnel);

                foreach (var nic in activeNics)
                {
                    string path = $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{nic.Id}";
                    SetDword(Registry.LocalMachine, path, "TcpAckFrequency", enable ? 1 : null);
                    SetDword(Registry.LocalMachine, path, "TCPNoDelay", enable ? 1 : null);
                    SetDword(Registry.LocalMachine, path, "TcpDelAckTicks", enable ? 0 : null);
                }

                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetMouseMenuDelayZeroAsync(bool enable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                using var desktopKey = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop", writable: true);
                desktopKey?.SetValue("MenuShowDelay", enable ? "0" : "400", RegistryValueKind.String);
                return true;
            }
            catch { return false; }
        }, ct);

    public async Task<bool> SetDefenderCpuLimit25PercentAsync(bool enable, CancellationToken ct = default)
    {
        int factor = enable ? 25 : 50;
        SetDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Scan", "AvgCPULoadFactor", factor);
        return await RunHiddenProcessAsync(
            "powershell.exe",
            $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"Set-MpPreference -ScanAvgCPULoadFactor {factor} -ErrorAction SilentlyContinue\"",
            ct).ConfigureAwait(false);
    }

    public async Task<bool> SetCoreIsolationVbsDisabledAsync(bool disable, CancellationToken ct = default)
    {
        SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", disable ? 0 : 1);
        SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", disable ? 0 : 1);
        string launchType = disable ? "off" : "auto";
        return await RunHiddenProcessAsync("bcdedit.exe", $"/set hypervisorlaunchtype {launchType}", ct).ConfigureAwait(false);
    }

    public Task<bool> SetSysMainDisabledAsync(bool disable, CancellationToken ct = default) =>
        ConfigureServiceAsync("SysMain", disable ? "disabled" : "auto", stopNow: disable, ct);

    public async Task<bool> SetStorageSenseAndSearchDisabledAsync(bool disable, CancellationToken ct = default)
    {
        SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy", "01", disable ? 0 : 1);
        return await ConfigureServiceAsync("WSearch", disable ? "disabled" : "delayed-auto", stopNow: disable, ct).ConfigureAwait(false);
    }

    public async Task<bool> SetFastStartupDisabledAsync(bool disable, CancellationToken ct = default)
    {
        SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", disable ? 0 : 1);
        await Task.CompletedTask;
        return true;
    }

    // =========================================================================
    // E) VARIOS (INTERFAZ & KERNEL)
    // =========================================================================

    public Task<bool> SetWin10ClassicContextMenuAsync(bool enable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                const string clsidPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";
                if (enable)
                {
                    using var inproc = Registry.CurrentUser.CreateSubKey($@"{clsidPath}\InprocServer32", writable: true);
                    inproc?.SetValue(string.Empty, string.Empty, RegistryValueKind.String);
                }
                else
                {
                    Registry.CurrentUser.DeleteSubKeyTree(clsidPath, throwOnMissingSubKey: false);
                }

                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetMpoDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                // OverlayTestMode = 5 desactiva Multiplane Overlay (MPO) en DWM
                SetDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", disable ? 5 : null);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetTaskbarEndTaskEnabledAsync(bool enable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", enable ? 1 : 0);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetExplorerHomeAndGalleryRemovedAsync(bool remove, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                int pinVal = remove ? 0 : 1;
                // CLSID Inicio (Home) y Galería (Gallery) en Windows 11
                SetDword(Registry.CurrentUser, @"Software\Classes\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}", "System.IsPinnedToNameSpaceTree", pinVal);
                SetDword(Registry.CurrentUser, @"Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}", "System.IsPinnedToNameSpaceTree", pinVal);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetTaskbarIconsLeftAsync(bool alignLeft, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                // TaskbarAl: 0 = Izquierda, 1 = Centro
                SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", alignLeft ? 0 : 1);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetNumLockAndStickyKeysDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                using (var kbd = Registry.CurrentUser.CreateSubKey(@"Control Panel\Keyboard", writable: true))
                {
                    kbd?.SetValue("InitialKeyboardIndicators", disable ? "0" : "2", RegistryValueKind.String);
                }

                using (var sticky = Registry.CurrentUser.CreateSubKey(@"Control Panel\Accessibility\StickyKeys", writable: true))
                {
                    sticky?.SetValue("Flags", disable ? "506" : "510", RegistryValueKind.String);
                }

                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetSystemDarkModeForcedAsync(bool forceDark, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                int val = forceDark ? 0 : 1;
                const string personalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                SetDword(Registry.CurrentUser, personalizePath, "AppsUseLightTheme", val);
                SetDword(Registry.CurrentUser, personalizePath, "SystemUsesLightTheme", val);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetShowFileExtensionsAndHiddenAsync(bool show, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                const string advPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                SetDword(Registry.CurrentUser, advPath, "HideFileExt", show ? 0 : 1);
                SetDword(Registry.CurrentUser, advPath, "Hidden", show ? 1 : 2);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetSnapLayoutsDisabledAsync(bool disable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                SetDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "EnableSnapAssistFlyout", disable ? 0 : 1);
                return true;
            }
            catch { return false; }
        }, ct);

    public Task<bool> SetDetailedBsodAndVerboseLogonAsync(bool enable, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            try
            {
                SetDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\CrashControl", "DisplayParameters", enable ? 1 : 0);
                SetDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", enable ? 1 : 0);
                return true;
            }
            catch { return false; }
        }, ct);

    // =========================================================================
    // HELPERS PRIVADOS SILENCIOSOS (CreateNoWindow = true)
    // =========================================================================

    private static void SetDword(RegistryKey root, string subKeyPath, string valueName, int? value)
    {
        using var key = root.CreateSubKey(subKeyPath, writable: true);
        if (key is null) return;

        if (value.HasValue)
            key.SetValue(valueName, value.Value, RegistryValueKind.DWord);
        else
            key.DeleteValue(valueName, throwOnMissingValue: false);
    }

    private static async Task<bool> RemoveAppxPackagesAsync(IEnumerable<string> patterns, CancellationToken ct)
    {
        var list = patterns.ToList();
        string filterAppx = string.Join(" -or ", list.Select(p => $"$_.Name -like '{p}'"));
        string filterProv = string.Join(" -or ", list.Select(p => $"$_.DisplayName -like '{p}'"));
        string script =
            $"Get-AppxPackage -AllUsers | Where-Object {{ {filterAppx} }} | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue; " +
            $"Get-AppxProvisionedPackage -Online | Where-Object {{ {filterProv} }} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue";

        return await RunHiddenProcessAsync(
            "powershell.exe",
            $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
            ct).ConfigureAwait(false);
    }

    private static async Task<bool> ConfigureServiceAsync(string serviceName, string startMode, bool stopNow, CancellationToken ct)
    {
        if (stopNow)
        {
            await RunHiddenProcessAsync("sc.exe", $"stop \"{serviceName}\"", ct).ConfigureAwait(false);
        }

        return await RunHiddenProcessAsync("sc.exe", $"config \"{serviceName}\" start= {startMode}", ct).ConfigureAwait(false);
    }

    private static async Task<bool> RunHiddenProcessAsync(string fileName, string arguments, CancellationToken ct)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            if (proc is null) return false;
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
