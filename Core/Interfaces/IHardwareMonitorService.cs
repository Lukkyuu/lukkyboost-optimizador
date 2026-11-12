using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.Core.Interfaces;

public interface IHardwareMonitorService : IDisposable
{
    event EventHandler<TelemetryMetrics>? MetricsUpdated;

    Task InitializeSensorsAsync(CancellationToken cancellationToken = default);
    Task<TelemetryMetrics> CaptureSnapshotAsync(CancellationToken cancellationToken = default);
    void StartPolling(TimeSpan interval);
    void StopPolling();
}

