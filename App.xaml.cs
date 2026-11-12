using System.Windows;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Services;
using LukkyBoost.App.ViewModels;
using LukkyBoost.App.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LukkyBoost.App;

public partial class App : Application
{
    private readonly IHost _host;

    public IServiceProvider Services => _host.Services;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // Servicios Core (Singletons)
                services.AddSingleton<IHardwareMonitorService, HardwareMonitorService>();
                services.AddSingleton<IGpuDriverService, GpuShaderService>();
                services.AddSingleton<ISystemOptimizerService, SystemOptimizationService>();
                services.AddSingleton<IBackupService, BackupService>();
                services.AddSingleton<IProfileService, ProfileService>();
                services.AddSingleton<IWingetToolboxService, WingetToolboxService>();

                // ViewModels MVVM
                services.AddSingleton<PcDashboardViewModel>();
                services.AddSingleton<ConfigViewModel>();
                services.AddSingleton<BackupViewModel>();
                services.AddSingleton<DriversViewModel>();
                services.AddSingleton<ToolboxViewModel>();
                services.AddSingleton<MainViewModel>();

                // Ventanas
                services.AddTransient<SplashScreenView>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var splash = _host.Services.GetRequiredService<SplashScreenView>();
        splash.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
        }

        base.OnExit(e);
    }
}
