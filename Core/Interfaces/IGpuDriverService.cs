using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.Core.Interfaces;

public interface IGpuDriverService
{
    Task<GpuDriverInfo> DetectActiveGpuAndShadersAsync(CancellationToken cancellationToken = default);

    Task<ShaderPurgeResult> PurgeShaderCacheAsync(
        ShaderCacheTarget target,
        CancellationToken cancellationToken = default);

    Task<bool> RestartVideoSubsystemHotAsync(CancellationToken cancellationToken = default);

    Task<bool> SetWindowsUpdateDriverBlockAsync(bool block, CancellationToken cancellationToken = default);
}

