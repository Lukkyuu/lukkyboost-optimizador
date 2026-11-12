namespace LukkyBoost.App.Core.Interfaces;

public interface ISystemOptimizerService
{
    Task<bool> ApplyTweakByIdAsync(string tweakId, bool enable, CancellationToken cancellationToken = default);

    // Métodos directos para el Motor de Perfiles y acciones rápidas
    Task<bool> SetWindowsAiDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> RemoveMicrosoftBloatwareAppsAsync(bool remove, CancellationToken ct = default);
    Task<bool> RemoveThirdPartyPromotedAppsAsync(bool remove, CancellationToken ct = default);
    Task<bool> RemoveOemCodecExtensionsAsync(bool remove, CancellationToken ct = default);
    Task<bool> RemoveXboxComponentsAsync(bool remove, CancellationToken ct = default);
    Task<bool> SetMicrosoftStoreDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetStoreAutoUpdatesDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> UninstallOneDriveAsync(bool uninstall, CancellationToken ct = default);
    Task<bool> SetEdgeBloatReducedAsync(bool reduce, CancellationToken ct = default);
    Task<bool> SetBraveBloatReducedAsync(bool reduce, CancellationToken ct = default);
    Task<bool> RemoveWindowsWidgetsAsync(bool remove, CancellationToken ct = default);

    // Privacidad
    Task<bool> SetGlobalAppPermissionsDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetWindowsUpdatePauseLimit20YearsAsync(bool enable, CancellationToken ct = default);
    Task<bool> SetWpbtDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetBitLockerAutoEncryptionDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetCloudSyncDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetActivityHistoryTimelineDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetNotificationCenterDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetOfflineMapsAutoDownloadDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> RemoveDefault0OobeUserAsync(bool remove, CancellationToken ct = default);
    Task<bool> SetLockScreenCamAndBiometricsDisabledAsync(bool disable, CancellationToken ct = default);

    // Telemetría
    Task<bool> SetWindowsTelemetryDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetThirdPartyTelemetryDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetWindowsUpdateDeliveryOptDisabledAsync(bool disable, CancellationToken ct = default);

    // Rendimiento
    Task<bool> SetServicesToManualModeAsync(bool enable, CancellationToken ct = default);
    Task<bool> SetWindowsTransparencyDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetHagsDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetIpv4PreferredAndTcpNoDelayAsync(bool enable, CancellationToken ct = default);
    Task<bool> SetMouseMenuDelayZeroAsync(bool enable, CancellationToken ct = default);
    Task<bool> SetDefenderCpuLimit25PercentAsync(bool enable, CancellationToken ct = default);
    Task<bool> SetCoreIsolationVbsDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetSysMainDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetStorageSenseAndSearchDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetFastStartupDisabledAsync(bool disable, CancellationToken ct = default);

    // Varios
    Task<bool> SetWin10ClassicContextMenuAsync(bool enable, CancellationToken ct = default);
    Task<bool> SetMpoDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetTaskbarEndTaskEnabledAsync(bool enable, CancellationToken ct = default);
    Task<bool> SetExplorerHomeAndGalleryRemovedAsync(bool remove, CancellationToken ct = default);
    Task<bool> SetTaskbarIconsLeftAsync(bool alignLeft, CancellationToken ct = default);
    Task<bool> SetNumLockAndStickyKeysDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetSystemDarkModeForcedAsync(bool forceDark, CancellationToken ct = default);
    Task<bool> SetShowFileExtensionsAndHiddenAsync(bool show, CancellationToken ct = default);
    Task<bool> SetSnapLayoutsDisabledAsync(bool disable, CancellationToken ct = default);
    Task<bool> SetDetailedBsodAndVerboseLogonAsync(bool enable, CancellationToken ct = default);
}
