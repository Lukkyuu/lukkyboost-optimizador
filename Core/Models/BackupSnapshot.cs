using System.Text.Json.Serialization;

namespace LukkyBoost.App.Core.Models;

public sealed record ServiceStateEntry(
    string ServiceName,
    string StartType,
    string Status);

public sealed record BackupSnapshot(
    string Id,
    DateTimeOffset CreatedAtUtc,
    string Description,
    bool WmiRestorePointCreated,
    string JsonFilePath,
    IReadOnlyList<string> ExportedRegFiles,
    IReadOnlyList<ServiceStateEntry> Services)
{
    [JsonIgnore]
    public string FormattedDate => CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
}

