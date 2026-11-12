using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LukkyBoost.App.Core.Enums;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;
using Microsoft.Win32;

namespace LukkyBoost.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly IProfileService _profileService;
    private readonly PcDashboardViewModel _pcDashboardVm;
    private readonly ConfigViewModel _configVm;
    private readonly BackupViewModel _backupVm;
    private readonly DriversViewModel _driversVm;
    private readonly ToolboxViewModel _toolboxVm;

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    [ObservableProperty]
    private NavigationTab _activeTab = NavigationTab.TelemetryDashboard;

    [ObservableProperty]
    private PerformanceProfile _activeProfile = PerformanceProfile.Normal;

    [ObservableProperty]
    private string _systemStatusText = "Sistema Listo";

    public MainViewModel(
        IProfileService profileService,
        PcDashboardViewModel pcDashboardVm,
        ConfigViewModel configVm,
        BackupViewModel backupVm,
        DriversViewModel driversVm,
        ToolboxViewModel toolboxVm)
    {
        _profileService = profileService;
        _pcDashboardVm = pcDashboardVm;
        _configVm = configVm;
        _backupVm = backupVm;
        _driversVm = driversVm;
        _toolboxVm = toolboxVm;

        _currentViewModel = _pcDashboardVm;
        _activeProfile = _profileService.CurrentProfile;
    }

    // -------------------------------------------------------------------------
    // BOTONES GLOBALES DE ACCIÓN RÁPIDA (DESMARCAR TODO, IMPORTAR, EXPORTAR)
    // -------------------------------------------------------------------------

    [RelayCommand]
    private void UncheckAll()
    {
        _configVm.UncheckAllTweaks();
        SystemStatusText = "Opciones desmarcadas";
    }

    [RelayCommand]
    private async Task ExportConfigAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Exportar configuración lukkyboost",
            Filter = "Configuración JSON (*.json)|*.json",
            FileName = $"lukkyboost_config_{DateTime.Now:yyyyMMdd}.json"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var payload = new LukkyConfigExport(
                ActiveProfile: _profileService.CurrentProfile.ToString(),
                EnabledTweakIds: _configVm.GetEnabledTweakIds());

            string json = JsonSerializer.Serialize(payload, JsonOpts);
            await File.WriteAllTextAsync(dialog.FileName, json);
            SystemStatusText = "Configuración exportada";
        }
        catch
        {
            SystemStatusText = "Error al exportar JSON";
        }
    }

    [RelayCommand]
    private async Task ImportConfigAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Importar configuración lukkyboost",
            Filter = "Configuración JSON (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            string json = await File.ReadAllTextAsync(dialog.FileName);
            var payload = JsonSerializer.Deserialize<LukkyConfigExport>(json, JsonOpts);
            if (payload is null) return;

            if (Enum.TryParse<PerformanceProfile>(payload.ActiveProfile, ignoreCase: true, out var parsedProfile))
            {
                ActiveProfile = parsedProfile;
                _pcDashboardVm.SyncProfileSelection(parsedProfile);
                await _profileService.ApplyProfileAsync(parsedProfile);
            }

            if (payload.EnabledTweakIds is not null)
            {
                await _configVm.ApplyImportedTweaksAsync(payload.EnabledTweakIds);
            }

            SystemStatusText = "Configuración importada";
        }
        catch
        {
            SystemStatusText = "Error al importar JSON";
        }
    }

    // -------------------------------------------------------------------------
    // NAVEGACIÓN DE LAS 9 VISTAS DEL SIDEBAR
    // -------------------------------------------------------------------------

    [RelayCommand]
    private void NavigateTelemetryDashboard()
    {
        ActiveTab = NavigationTab.TelemetryDashboard;
        CurrentViewModel = _pcDashboardVm;
    }

    [RelayCommand]
    private void NavigateDebloat()
    {
        ActiveTab = NavigationTab.Debloat;
        _configVm.ShowCategory("Debloat", "Debloat");
        CurrentViewModel = _configVm;
    }

    [RelayCommand]
    private void NavigatePrivacy()
    {
        ActiveTab = NavigationTab.Privacy;
        _configVm.ShowCategory("Privacidad", "Privacidad");
        CurrentViewModel = _configVm;
    }

    [RelayCommand]
    private void NavigateTelemetryControl()
    {
        ActiveTab = NavigationTab.TelemetryControl;
        _configVm.ShowCategory("Telemetria", "Telemetría");
        CurrentViewModel = _configVm;
    }

    [RelayCommand]
    private void NavigatePerformance()
    {
        ActiveTab = NavigationTab.Performance;
        _configVm.ShowCategory("Rendimiento", "Rendimiento");
        CurrentViewModel = _configVm;
    }

    [RelayCommand]
    private void NavigateMisc()
    {
        ActiveTab = NavigationTab.Misc;
        _configVm.ShowCategory("Varios", "Varios");
        CurrentViewModel = _configVm;
    }

    [RelayCommand]
    private async Task NavigateExploreAppsAsync()
    {
        ActiveTab = NavigationTab.ExploreApps;
        CurrentViewModel = _toolboxVm;
        await _toolboxVm.RefreshInstalledStatusAsync();
    }

    [RelayCommand]
    private async Task NavigateDriversAsync()
    {
        ActiveTab = NavigationTab.Drivers;
        CurrentViewModel = _driversVm;
        await _driversVm.RefreshGpuDataAsync();
    }

    [RelayCommand]
    private async Task NavigateBackupAsync()
    {
        ActiveTab = NavigationTab.Backup;
        CurrentViewModel = _backupVm;
        await _backupVm.LoadSnapshotsAsync();
    }
}
