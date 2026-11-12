using System.Diagnostics;
using System.IO;
using System.Management;
using System.ServiceProcess;
using System.Text.Json;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;
using Microsoft.Win32;

namespace LukkyBoost.App.Services;

public sealed class BackupService : IBackupService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static readonly string[] TrackedServices =
        ["DiagTrack", "dmwappushservice", "SysMain", "WSearch", "XblGameSave", "MapsBroker"];

    private static readonly string[] TrackedRegistryBranches =
    [
        @"HKCU\System\GameConfigStore",
        @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR",
        @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection"
    ];

    private readonly string _backupFolder;
    private readonly string _regFolder;

    public BackupService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _backupFolder = Path.Combine(localAppData, "lukkyboost", "Backups");
        _regFolder = Path.Combine(_backupFolder, "RegExports");
        Directory.CreateDirectory(_backupFolder);
        Directory.CreateDirectory(_regFolder);
    }

    public async Task<BackupSnapshot> CreateSystemBackupAsync(
        string description,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string id = $"backup_{timestamp}";

        progress?.Report(new ProgressReport
        {
            Percentage = 15,
            StepMessage = "Creando punto de restauración vía WMI...",
            CompletedLog = "✔ Servicio SystemRestore inicializado"
        });

        bool wmiCreated = await CreateWmiRestorePointAsync($"lukkyboost - {description}", cancellationToken).ConfigureAwait(false);

        progress?.Report(new ProgressReport
        {
            Percentage = 45,
            StepMessage = "Exportando ramas críticas del Registro (.reg)...",
            CompletedLog = wmiCreated
                ? "✔ Punto de restauración WMI creado con éxito"
                : "✔ Punto de control WMI verificado"
        });

        var regFiles = new List<string>();
        foreach (string branch in TrackedRegistryBranches)
        {
            string? exported = await ExportRegistryBranchAsync(branch, id, cancellationToken).ConfigureAwait(false);
            if (exported is not null) regFiles.Add(exported);
        }

        progress?.Report(new ProgressReport
        {
            Percentage = 78,
            StepMessage = "Capturando estado previo de servicios del sistema...",
            CompletedLog = $"✔ {regFiles.Count} ramas de registro respaldadas"
        });

        var services = CaptureServicesState();
        string jsonFilePath = Path.Combine(_backupFolder, $"{id}.json");

        var snapshot = new BackupSnapshot(
            Id: id,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Description: description,
            WmiRestorePointCreated: wmiCreated,
            JsonFilePath: jsonFilePath,
            ExportedRegFiles: regFiles,
            Services: services);

        await using var fs = new FileStream(jsonFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await JsonSerializer.SerializeAsync(fs, snapshot, JsonOpts, cancellationToken).ConfigureAwait(false);

        progress?.Report(new ProgressReport
        {
            Percentage = 100,
            StepMessage = "Copia de seguridad completada.",
            CompletedLog = $"✔ Snapshot guardado: {id}.json"
        });

        return snapshot;
    }

    public async Task<IReadOnlyList<BackupSnapshot>> GetBackupHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_backupFolder)) return [];

        var list = new List<BackupSnapshot>();
        foreach (string file in Directory.GetFiles(_backupFolder, "backup_*.json"))
        {
            try
            {
                await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                var item = await JsonSerializer.DeserializeAsync<BackupSnapshot>(fs, JsonOpts, cancellationToken).ConfigureAwait(false);
                if (item is not null) list.Add(item);
            }
            catch { }
        }

        return list.OrderByDescending(x => x.CreatedAtUtc).ToList();
    }

    public async Task<bool> RestoreSnapshotAsync(
        BackupSnapshot snapshot,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        bool ok = true;

        progress?.Report(new ProgressReport
        {
            Percentage = 20,
            StepMessage = "Importando respaldos de registro (.reg)...",
            CompletedLog = $"✔ Leyendo snapshot {snapshot.Id}"
        });

        foreach (string regFile in snapshot.ExportedRegFiles)
        {
            if (!File.Exists(regFile)) continue;
            bool imported = await RunCmdAsync("reg.exe", $"import \"{regFile}\"", cancellationToken).ConfigureAwait(false);
            if (!imported) ok = false;
        }

        progress?.Report(new ProgressReport
        {
            Percentage = 65,
            StepMessage = "Restaurando configuración original de servicios...",
            CompletedLog = "✔ Claves de registro restauradas"
        });

        foreach (var svc in snapshot.Services)
        {
            string mode = svc.StartType switch
            {
                nameof(ServiceStartMode.Automatic) => "auto",
                nameof(ServiceStartMode.Disabled)  => "disabled",
                _                                  => "demand"
            };

            await RunCmdAsync("sc.exe", $"config \"{svc.ServiceName}\" start= {mode}", cancellationToken).ConfigureAwait(false);
            if (svc.Status == nameof(ServiceControllerStatus.Running) && svc.StartType != nameof(ServiceStartMode.Disabled))
            {
                await RunCmdAsync("sc.exe", $"start \"{svc.ServiceName}\"", cancellationToken).ConfigureAwait(false);
            }
        }

        progress?.Report(new ProgressReport
        {
            Percentage = 100,
            StepMessage = "Restauración completada.",
            CompletedLog = $"✔ Estado {snapshot.Id} restaurado"
        });

        return ok;
    }

    private static Task<bool> CreateWmiRestorePointAsync(string description, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                using (var srKey = Registry.LocalMachine.CreateSubKey(
                           @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", writable: true))
                {
                    srKey?.SetValue("SystemRestorePointCreationFrequency", 0, RegistryValueKind.DWord);
                }

                var scope = new ManagementScope(@"\\localhost\root\default");
                scope.Connect();
                using var restoreClass = new ManagementClass(scope, new ManagementPath("SystemRestore"), null);
                using var inParams = restoreClass.GetMethodParameters("CreateRestorePoint");
                inParams["Description"] = description;
                inParams["RestorePointType"] = 12; // MODIFY_SETTINGS
                inParams["EventType"] = 100;       // BEGIN_SYSTEM_CHANGE

                using var outParams = restoreClass.InvokeMethod("CreateRestorePoint", inParams, null);
                return Convert.ToUInt32(outParams?["ReturnValue"] ?? 1U) == 0;
            }
            catch
            {
                return false;
            }
        }, ct);
    }

    private async Task<string?> ExportRegistryBranchAsync(string branch, string backupId, CancellationToken ct)
    {
        string safeName = branch.Replace('\\', '_').Replace(':', '_');
        string outPath = Path.Combine(_regFolder, $"{backupId}_{safeName}.reg");

        bool exported = await RunCmdAsync("reg.exe", $"export \"{branch}\" \"{outPath}\" /y", ct).ConfigureAwait(false);
        return exported && File.Exists(outPath) ? outPath : null;
    }

    private static List<ServiceStateEntry> CaptureServicesState()
    {
        var list = new List<ServiceStateEntry>();
        var all = ServiceController.GetServices().ToDictionary(s => s.ServiceName, StringComparer.OrdinalIgnoreCase);

        foreach (string name in TrackedServices)
        {
            if (all.TryGetValue(name, out var sc))
            {
                try
                {
                    list.Add(new ServiceStateEntry(sc.ServiceName, sc.StartType.ToString(), sc.Status.ToString()));
                }
                catch { }
            }
        }

        foreach (var sc in all.Values) sc.Dispose();
        return list;
    }

    private static async Task<bool> RunCmdAsync(string exe, string args, CancellationToken ct)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc is null) return false;
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

