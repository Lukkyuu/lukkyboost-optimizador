using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.Core.Interfaces;

public interface IHardwareMonitorService : IDisposable
{
    event Action<HardwareSnapshot>? OnTelemetryUpdated;

    Task InitializeSensorsAsync(CancellationToken cancellationToken = default);
    Task<HardwareSnapshot> CaptureSnapshotAsync(CancellationToken cancellationToken = default);
    void StartPolling(TimeSpan interval);
    void StopPolling();
}
