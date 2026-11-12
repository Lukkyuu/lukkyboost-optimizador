using System.Management;
using Microsoft.Win32;

namespace LukkyBoost.App.Services;

public static class SystemRestoreService
{
    /// <summary>
    /// Crea un Punto de Restauración del sistema en Windows 10/11 mediante WMI (root\default:SystemRestore).
    /// </summary>
    public static bool CreateRestorePoint(string description)
    {
        try
        {
            using (var srKey = Registry.LocalMachine.CreateSubKey(
                       @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", writable: true))
            {
                // Permite crear múltiples puntos de restauración sin la restricción de 24 horas de Windows
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
    }
}

