namespace LukkyBoost.App.Core.Models;

/// <summary>
/// Snapshot inmutable de telemetría de hardware capturado en segundo plano.
/// </summary>
public sealed record HardwareSnapshot(
    string CpuModel,
    double CpuClockMhz,
    double CpuLoadPercent,
    double CpuTempCelsius,
    string GpuModel,
    string GpuVendor,
    string GpuDriverVersion,
    double GpuLoadPercent,
    double GpuVramUsedMb,
    double GpuVramTotalMb,
    double GpuTempCelsius,
    double RamUsedGb,
    double RamTotalGb,
    double RamUsagePercent,
    int RamSpeedMhz,
    string StorageModel,
    double StorageTransferRateMbPerSec,
    double StorageFreeSpaceGb,
    double StorageTotalSpaceGb);
