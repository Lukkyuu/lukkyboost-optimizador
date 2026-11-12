using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LukkyBoost.App.Core.Models;

public sealed partial class TweakToggleItem : ObservableObject
{
    private readonly Func<string, bool, Task<bool>> _applyAction;
    private readonly Action<string>? _statusCallback;

    public string Id { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Icon { get; init; } = "⚙️";
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isActive;

    public TweakToggleItem(
        string id,
        string category,
        string icon,
        string title,
        string description,
        Func<string, bool, Task<bool>> applyAction,
        Action<string>? statusCallback = null)
    {
        Id = id;
        Category = category;
        Icon = icon;
        Title = title;
        Description = description;
        _applyAction = applyAction;
        _statusCallback = statusCallback;
    }

    public void SetStateSilently(bool active)
    {
        #pragma warning disable MVVMTK0034
        _isActive = active;
        #pragma warning restore MVVMTK0034
        OnPropertyChanged(nameof(IsActive));
    }

    [RelayCommand]
    private async Task ExecuteToggleAsync()
    {
        bool ok = await _applyAction(Id, IsActive);
        _statusCallback?.Invoke(ok
            ? $"{Title}: {(IsActive ? "Activado" : "Revertido")}"
            : $"No se pudo aplicar '{Title}'");
    }
}

public sealed record LukkyConfigExport(
    string ActiveProfile,
    IReadOnlyList<string> EnabledTweakIds);

