using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.Core.Interfaces;

public interface IBackupService
{
    Task<BackupSnapshot> CreateSystemBackupAsync(
        string description,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupSnapshot>> GetBackupHistoryAsync(
        CancellationToken cancellationToken = default);

    Task<bool> RestoreSnapshotAsync(
        BackupSnapshot snapshot,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}
