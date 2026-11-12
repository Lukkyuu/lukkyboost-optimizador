using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.Core.Interfaces;

public interface IBackupService
{
    Task<BackupSnapshot> CreateSystemBackupAsync(string description, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupSnapshot>> GetBackupHistoryAsync(CancellationToken cancellationToken = default);
    Task<bool> RestoreSnapshotAsync(BackupSnapshot snapshot, CancellationToken cancellationToken = default);
}

