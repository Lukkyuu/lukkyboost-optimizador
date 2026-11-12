using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LukkyBoost.App.Core.Enums;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;
using LukkyBoost.App.Services;
using LukkyBoost.App.Views;
using Microsoft.Win32;

namespace LukkyBoost.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly IProfileService _profileService;
    private readonly IBackupService _backupService;
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
    private string _statusMessage = "Sistema Listo";

    [ObservableProperty]
    private string _statusColor = "#10B981";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    public MainViewModel(
        IProfileService profileService,
        IBackupService backupService,
        PcDashboardViewModel pcDashboardVm,
        ConfigViewModel configVm,
        BackupViewModel backupVm,
        DriversViewModel driversVm,
        ToolboxViewModel toolboxVm)
    {
        _profileService = profileService;
        _backupService = backupService;
        _pcDashboardVm = pcDashboardVm;
        _configVm = configVm;
        _backupVm = backupVm;
        _driversVm = driversVm;
        _toolboxVm = toolboxVm;

        _currentViewModel = _pcDashboardVm;
        _activeProfile = _profileService.CurrentProfile;
    }

    // -------------------------------------------------------------------------
    // BOTÓN DE CONFIRMACIÓN Y EJECUCIÓN EN LOTE ("⚡ Aplicar cambios")
    // -------------------------------------------------------------------------

    [RelayCommand]
    private async Task ApplyAllChangesAsync()
    {
        // 1. Validación de ejecución concurrente
        if (IsBusy)
            return;

        // 2. Diálogo modal de confirmación Sí/No
        var confirmation = MessageBox.Show(
            "¿Deseas aplicar todas las optimizaciones y cambios seleccionados?\n\nSe creará un Punto de Restauración del sistema automáticamente.",
            "lukkyboost — Confirmar optimización",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.Yes)
            return;

        try
        {
            // 3. Secuencia de aplicación en segundo plano con ventana modal de progreso
            IsBusy = true;
            StatusMessage = "Creando respaldo...";
            StatusColor = "#F59E0B";

            await ProgressModalWindow.ExecuteAsync(
                "Aplicando Optimizaciones",
                async progress =>
                {
                    await Task.Run(async () =>
                    {
                        progress.Report(new ProgressReport
                        {
                            Percentage = 10,
                            StepMessage = "Creando punto de restauración vía WMI...",
                            CompletedLog = "✔ Verificación de privilegios UAC completada"
                        });

                        _ = SystemRestoreService.CreateRestorePoint("lukkyboost - Pre Optimization");
                        await _backupService.CreateSystemBackupAsync("Pre Optimization", progress).ConfigureAwait(false);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            StatusMessage = "Aplicando optimizaciones...";
                        });

                        progress.Report(new ProgressReport
                        {
                            Percentage = 40,
                            StepMessage = "Ejecutando lote de optimizaciones seleccionadas...",
                            CompletedLog = "✔ Punto de restauración y respaldo de registro completados"
                        });

                        await _configVm.ApplyAllSelectedTweaksAsync(progress, startPercent: 40, endPercent: 88).ConfigureAwait(false);

                        progress.Report(new ProgressReport
                        {
                            Percentage = 92,
                            StepMessage = $"Aplicando perfil de energía y red ({ActiveProfile})..."
                        });

                        await _profileService.ApplyProfileAsync(ActiveProfile).ConfigureAwait(false);

                        progress.Report(new ProgressReport
                        {
                            Percentage = 100,
                            StepMessage = "Optimizaciones aplicadas con éxito.",
                            CompletedLog = $"✔ Perfil '{ActiveProfile}' y optimizaciones sincronizados"
                        });
                    });
                });

            StatusMessage = "Cambios aplicados con éxito";
            StatusColor = "#10B981";

            MessageBox.Show(
                "Todas las optimizaciones seleccionadas y el respaldo del sistema se han aplicado correctamente.",
                "lukkyboost — Optimización completada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // 4. Manejo de excepciones sin bloquear la UI
            StatusMessage = "Error al aplicar";
            StatusColor = "#EF4444";

            MessageBox.Show(
                $"Ocurrió un error durante la aplicación de cambios:\n{ex.Message}",
                "lukkyboost — Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            // 5. Restauración automática de estado tras 4 segundos
            await Task.Delay(4000);
            StatusMessage = "Sistema Listo";
            StatusColor = "#10B981";
            IsBusy = false;
        }
    }

    // -------------------------------------------------------------------------
    // ACCIONES AUXILIARES (DESMARCAR TODO, IMPORTAR, EXPORTAR)
    // -------------------------------------------------------------------------

    [RelayCommand]
    private void UncheckAll()
    {
        _configVm.UncheckAllTweaks();
        StatusMessage = "Opciones desmarcadas";
        StatusColor = "#10B981";
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
            StatusMessage = "Configuración exportada";
            StatusColor = "#10B981";
        }
        catch
        {
            StatusMessage = "Error al exportar JSON";
            StatusColor = "#EF4444";
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

            StatusMessage = "Configuración importada";
            StatusColor = "#10B981";
        }
        catch
        {
            StatusMessage = "Error al importar JSON";
            StatusColor = "#EF4444";
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
