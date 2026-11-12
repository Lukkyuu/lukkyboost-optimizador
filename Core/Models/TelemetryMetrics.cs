namespace LukkyBoost.App.Core.Models;

public sealed record TelemetryMetrics(
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

