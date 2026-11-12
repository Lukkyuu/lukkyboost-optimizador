using System.Security.Principal;
using System.Windows;
using System.Windows.Media.Animation;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LukkyBoost.App.Views;

public partial class SplashScreenView : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHardwareMonitorService _monitorService;
    private readonly IProfileService _profileService;

    public SplashScreenView(
        IServiceProvider serviceProvider,
        IHardwareMonitorService monitorService,
        IProfileService profileService)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _monitorService = monitorService;
        _profileService = profileService;

        Loaded += OnSplashLoadedAsync;
    }

    private async void OnSplashLoadedAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            // 1. Verificar permisos UAC
            PreloadStatusText.Text = "Verificando permisos UAC...";
            PreloadProgressBar.Value = 30;
            await Task.Delay(180);

            _ = IsRunningAsAdministrator();

            // 2. Comprobación del hardware base
            PreloadStatusText.Text = "Detectando componentes de hardware...";
            PreloadProgressBar.Value = 65;
            await _monitorService.InitializeSensorsAsync();
            var initialMetrics = await _monitorService.CaptureSnapshotAsync();

            // 3. Lectura del último perfil aplicado
            PreloadStatusText.Text = "Cargando perfil de rendimiento...";
            PreloadProgressBar.Value = 90;
            var savedProfile = await _profileService.LoadLastSavedProfileAsync();

            var pcVm = _serviceProvider.GetRequiredService<PcDashboardViewModel>();
            pcVm.SyncProfileSelection(savedProfile);
            pcVm.ApplyMetrics(initialMetrics);

            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            mainVm.ActiveProfile = savedProfile;

            PreloadProgressBar.Value = 100;
            await Task.Delay(220);

            // 4. Cierre suave con Fade-Out y apertura fluida de MainWindow
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = mainVm;
            mainWindow.Show();

            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(320));
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
        catch
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();
            Close();
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
