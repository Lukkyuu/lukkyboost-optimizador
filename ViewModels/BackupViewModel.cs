using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.ViewModels;

public sealed partial class BackupViewModel : ObservableObject
{
    private readonly IBackupService _backupService;

    [ObservableProperty] private string _backupDescription = "Punto de control manual antes de optimizar";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Crea un Punto de Restauración WMI + Snapshot JSON antes de realizar cambios.";

    public ObservableCollection<BackupSnapshot> Snapshots { get; } = [];

    public BackupViewModel(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [RelayCommand]
    public async Task LoadSnapshotsAsync()
    {
        var items = await _backupService.GetBackupHistoryAsync();
        Snapshots.Clear();
        foreach (var item in items) Snapshots.Add(item);
    }

    [RelayCommand]
    private async Task CreateBackupNowAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            StatusMessage = "Creando Punto de Restauración WMI, exportando .reg y guardando estado de servicios...";
            var created = await _backupService.CreateSystemBackupAsync(BackupDescription);
            await LoadSnapshotsAsync();

            StatusMessage = created.WmiRestorePointCreated
                ? $"Snapshot '{created.Id}' y Punto de Restauración de Windows creados con éxito."
                : $"Snapshot JSON/REG '{created.Id}' creado localmente.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreSnapshotAsync(BackupSnapshot? snapshot)
    {
        if (snapshot is null || IsBusy) return;
        IsBusy = true;

        try
        {
            StatusMessage = $"Restaurando servicios y claves de registro desde '{snapshot.Id}'...";
            bool ok = await _backupService.RestoreSnapshotAsync(snapshot);
            StatusMessage = ok
                ? $"Estado previo '{snapshot.Id}' restaurado satisfactoriamente."
                : $"Restauración de '{snapshot.Id}' finalizada con advertencias.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

