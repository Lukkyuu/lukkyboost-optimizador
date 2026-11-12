using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.ViewModels;

public sealed partial class ToolboxViewModel : ObservableObject
{
    private readonly IWingetToolboxService _toolboxService;
    private readonly List<ToolboxAppItem> _allCatalog;

    [ObservableProperty]
    private string _selectedProvider = "Winget";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Listo.";

    public IReadOnlyList<string> Providers { get; } = ["Winget", "Chocolatey"];

    public ObservableCollection<ToolboxAppItem> ControladoresApps { get; } = [];
    public ObservableCollection<ToolboxAppItem> NavegadoresApps { get; } = [];
    public ObservableCollection<ToolboxAppItem> CompresionApps { get; } = [];
    public ObservableCollection<ToolboxAppItem> JuegosApps { get; } = [];
    public ObservableCollection<ToolboxAppItem> UtilidadesApps { get; } = [];

    public ToolboxViewModel(IWingetToolboxService toolboxService)
    {
        _toolboxService = toolboxService;
        _allCatalog = _toolboxService.GetCatalog().ToList();
        ApplyFilter();
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        string q = SearchQuery?.Trim() ?? string.Empty;
        bool Matches(ToolboxAppItem item) =>
            string.IsNullOrEmpty(q) ||
            item.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            item.Description.Contains(q, StringComparison.OrdinalIgnoreCase);

        SyncGroup(ControladoresApps, "Controladores", Matches);
        SyncGroup(NavegadoresApps, "Navegadores", Matches);
        SyncGroup(CompresionApps, "Compresión", Matches);
        SyncGroup(JuegosApps, "Juegos", Matches);
        SyncGroup(UtilidadesApps, "Utilidades", Matches);
    }

    private void SyncGroup(ObservableCollection<ToolboxAppItem> target, string category, Func<ToolboxAppItem, bool> predicate)
    {
        target.Clear();
        foreach (var item in _allCatalog.Where(a => a.Category == category && predicate(a)))
        {
            target.Add(item);
        }
    }

    [RelayCommand]
    public async Task RefreshInstalledStatusAsync()
    {
        StatusMessage = "Comprobando paquetes instalados...";
        await _toolboxService.RefreshInstallationStatesAsync(_allCatalog);
        StatusMessage = "Catálogo verificado.";
    }

    [RelayCommand]
    private async Task UpgradeAllAppsAsync()
    {
        StatusMessage = $"Actualizando todas las aplicaciones mediante {SelectedProvider}...";
        bool ok = await _toolboxService.UpgradeAllInstalledAppsAsync(SelectedProvider);
        StatusMessage = ok
            ? "Aplicaciones actualizadas correctamente."
            : "Finalizó el proceso de actualización.";
    }

    [RelayCommand]
    private async Task InstallOrLaunchAppAsync(ToolboxAppItem? item)
    {
        if (item is null || item.IsWorking) return;

        StatusMessage = $"Instalando '{item.Name}' mediante {SelectedProvider}...";
        bool ok = await _toolboxService.InstallOrLaunchAsync(item, SelectedProvider);
        StatusMessage = ok
            ? $"'{item.Name}' instalado correctamente."
            : $"No se pudo instalar '{item.Name}'.";
    }
}
