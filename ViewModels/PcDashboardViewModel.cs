using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LukkyBoost.App.Core.Enums;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;
using SkiaSharp;

namespace LukkyBoost.App.ViewModels;

public sealed partial class PcDashboardViewModel : ObservableObject, IDisposable
{
    private const int MaxBufferPoints = 20;

    private readonly IHardwareMonitorService _monitorService;
    private readonly IProfileService _profileService;
    private readonly DispatcherTimer _dispatcherTimer;

    // Búferes circulares de 20 muestras
    private readonly ObservableCollection<double> _cpuHistory = [];
    private readonly ObservableCollection<double> _gpuHistory = [];
    private readonly ObservableCollection<double> _ramHistory = [];
    private readonly ObservableCollection<double> _storageHistory = [];

    // Estado de los RadioButtons de Perfiles (56px+)
    [ObservableProperty] private bool _isJuegosSelected;
    [ObservableProperty] private bool _isCodigoSelected;
    [ObservableProperty] private bool _isOficinaSelected;
    [ObservableProperty] private bool _isNormalSelected = true;

    // Métricas en vivo
    [ObservableProperty] private string _cpuModel = "Detectando CPU...";
    [ObservableProperty] private double _cpuClockMhz;
    [ObservableProperty] private double _cpuLoadPercent;
    [ObservableProperty] private double _cpuTempCelsius;

    [ObservableProperty] private string _gpuModel = "Detectando GPU...";
    [ObservableProperty] private string _gpuDriverVersion = "--";
    [ObservableProperty] private double _gpuLoadPercent;
    [ObservableProperty] private double _gpuVramUsedMb;
    [ObservableProperty] private double _gpuVramTotalMb;
    [ObservableProperty] private double _gpuTempCelsius;

    [ObservableProperty] private double _ramUsedGb;
    [ObservableProperty] private double _ramTotalGb;
    [ObservableProperty] private double _ramUsagePercent;
    [ObservableProperty] private int _ramSpeedMhz;

    [ObservableProperty] private string _storageModel = "SSD Principal";
    [ObservableProperty] private double _storageTransferMbPerSec;
    [ObservableProperty] private double _storageFreeSpaceGb;
    [ObservableProperty] private double _storageTotalSpaceGb;

    // Series LiveCharts2
    public ISeries[] CpuSeries { get; }
    public ISeries[] GpuSeries { get; }
    public ISeries[] RamSeries { get; }
    public ISeries[] StorageSeries { get; }

    public Axis[] HiddenXAxis { get; } = [new Axis { IsVisible = false }];
    public Axis[] PercentageYAxis { get; } = [new Axis { MinLimit = 0, MaxLimit = 100, IsVisible = false }];
    public Axis[] ThroughputYAxis { get; } = [new Axis { MinLimit = 0, IsVisible = false }];

    public PcDashboardViewModel(
        IHardwareMonitorService monitorService,
        IProfileService profileService)
    {
        _monitorService = monitorService;
        _profileService = profileService;

        for (int i = 0; i < MaxBufferPoints; i++)
        {
            _cpuHistory.Add(0);
            _gpuHistory.Add(0);
            _ramHistory.Add(0);
            _storageHistory.Add(0);
        }

        // CPU: Cian eléctrico (#38BDF8)
        CpuSeries = CreateGradientSeries(_cpuHistory, new SKColor(0x38, 0xBD, 0xF8));
        // GPU: Índigo (#6366F1)
        GpuSeries = CreateGradientSeries(_gpuHistory, new SKColor(0x63, 0x66, 0xF1));
        // RAM: Azul (#007AFF)
        RamSeries = CreateGradientSeries(_ramHistory, new SKColor(0x00, 0x7A, 0xFF));
        // Almacenamiento: Esmeralda (#10B981)
        StorageSeries = CreateGradientSeries(_storageHistory, new SKColor(0x10, 0xB9, 0x81));

        _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(1000)
        };
        _dispatcherTimer.Tick += OnDispatcherTimerTickAsync;
        _dispatcherTimer.Start();
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

    private async void OnDispatcherTimerTickAsync(object? sender, EventArgs e)
    {
        try
        {
            var snapshot = await _monitorService.CaptureSnapshotAsync();
            ApplyMetrics(snapshot);
        }
        catch
        {
            // Ignorar fallos transitorios de lectura
        }
    }

    public void ApplyMetrics(TelemetryMetrics m)
    {
        CpuModel = m.CpuModel;
        CpuClockMhz = m.CpuClockMhz;
        CpuLoadPercent = m.CpuLoadPercent;
        CpuTempCelsius = m.CpuTempCelsius;

        GpuModel = m.GpuModel;
        GpuDriverVersion = m.GpuDriverVersion;
        GpuLoadPercent = m.GpuLoadPercent;
        GpuVramUsedMb = m.GpuVramUsedMb;
        GpuVramTotalMb = m.GpuVramTotalMb;
        GpuTempCelsius = m.GpuTempCelsius;

        RamUsedGb = m.RamUsedGb;
        RamTotalGb = m.RamTotalGb;
        RamUsagePercent = m.RamUsagePercent;
        RamSpeedMhz = m.RamSpeedMhz;

        StorageModel = m.StorageModel;
        StorageTransferMbPerSec = m.StorageTransferRateMbPerSec;
        StorageFreeSpaceGb = m.StorageFreeSpaceGb;
        StorageTotalSpaceGb = m.StorageTotalSpaceGb;

        PushSample(_cpuHistory, m.CpuLoadPercent);
        PushSample(_gpuHistory, m.GpuLoadPercent);
        PushSample(_ramHistory, m.RamUsagePercent);
        PushSample(_storageHistory, m.StorageTransferRateMbPerSec);
    }

    private static void PushSample(ObservableCollection<double> buffer, double value)
    {
        if (buffer.Count >= MaxBufferPoints)
        {
            buffer.RemoveAt(0);
        }

        buffer.Add(value);
    }

    private static ISeries[] CreateGradientSeries(ObservableCollection<double> values, SKColor color)
    {
        return
        [
            new LineSeries<double>
            {
                Values = values,
                GeometrySize = 0,
                LineSmoothness = 0.65,
                Stroke = new SolidColorPaint(color, 2.5f),
                Fill = new LinearGradientPaint(
                    [color.WithAlpha(110), color.WithAlpha(0)],
                    new SKPoint(0.5f, 0f),
                    new SKPoint(0.5f, 1f))
            }
        ];
    }

    public void Dispose()
    {
        _dispatcherTimer.Stop();
        _dispatcherTimer.Tick -= OnDispatcherTimerTickAsync;
    }
}
