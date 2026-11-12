using CommunityToolkit.Mvvm.ComponentModel;

namespace LukkyBoost.App.Core.Models;

public sealed partial class ToolboxAppItem : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string WingetId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = "📦";
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ExecutableHint { get; init; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    [NotifyPropertyChangedFor(nameof(ActionButtonText))]
    private bool _isInstalled;

    [ObservableProperty]
    private bool _isWorking;

    public string StatusLabel => IsInstalled ? "Instalado" : "Disponible";
    public string ActionButtonText => IsInstalled ? "Lanzar / Actualizar" : "Instalar con Winget";
}

