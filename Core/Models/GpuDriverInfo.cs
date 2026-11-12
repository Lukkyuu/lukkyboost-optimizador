namespace LukkyBoost.App.Core.Models;

public enum ShaderCacheTarget
{
    NvidiaDxAndGl,
    AmdAndD3D,
    AllCaches
}

public sealed record GpuDriverInfo(
    string Vendor,
    string ModelName,
    string InstalledDriverVersion,
    string WmiRawVersion,
    string OfficialDriverUrl,
    double NvidiaShaderCacheSizeMb,
    double AmdAndD3DShaderCacheSizeMb,
    bool WindowsUpdateDriverBlocked);

public sealed record ShaderPurgeResult(
    ShaderCacheTarget Target,
    double MegabytesFreed,
    int FilesDeleted,
    int LockedFilesSkipped);

