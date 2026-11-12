using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.Core.Interfaces;

public interface IWingetToolboxService
{
    IReadOnlyList<ToolboxAppItem> GetCatalog();
    Task RefreshInstallationStatesAsync(IEnumerable<ToolboxAppItem> items, CancellationToken cancellationToken = default);
    Task<bool> InstallOrLaunchAsync(ToolboxAppItem item, string provider = "Winget", CancellationToken cancellationToken = default);
    Task<bool> UpgradeAllInstalledAppsAsync(string provider = "Winget", CancellationToken cancellationToken = default);
}
