using System.Diagnostics;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.Services;

public sealed class WingetToolboxService : IWingetToolboxService
{
    public IReadOnlyList<ToolboxAppItem> GetCatalog() =>
    [
        // Controladores & Diagnóstico
        new() { Id = "hwinfo64",        WingetId = "REALiX.HWiNFO",                     Name = "HWiNFO64",                 IconGlyph = "🌡️", Category = "Controladores", Description = "Sensores VRM, temperaturas de HotSpot y voltajes en tiempo real." },
        new() { Id = "crystaldiskinfo", WingetId = "CrystalDewWorld.CrystalDiskInfo",   Name = "CrystalDiskInfo",          IconGlyph = "💿", Category = "Controladores", Description = "Estado de salud S.M.A.R.T., TBW y temperatura de unidades NVMe/SSD." },
        new() { Id = "cpuz",            WingetId = "CPUID.CPU-Z",                       Name = "CPU-Z",                    IconGlyph = "🔲", Category = "Controladores", Description = "Identificación exacta de procesador, placa base y timings de RAM." },
        new() { Id = "gpuz",            WingetId = "TechPowerUp.GPU-Z",                 Name = "GPU-Z",                    IconGlyph = "🎮", Category = "Controladores", Description = "Inspección de vBIOS, sensores PCIe y memoria de video." },

        // Navegadores
        new() { Id = "brave",           WingetId = "Brave.Brave",                       Name = "Brave Browser",            IconGlyph = "🦁", Category = "Navegadores",   Description = "Navegador Chromium centrado en bloqueo nativo de rastreadores." },
        new() { Id = "firefox",         WingetId = "Mozilla.Firefox",                   Name = "Mozilla Firefox",          IconGlyph = "🦊", Category = "Navegadores",   Description = "Navegador independiente de código abierto con motor Gecko." },
        new() { Id = "chrome",          WingetId = "Google.Chrome",                     Name = "Google Chrome",            IconGlyph = "🌐", Category = "Navegadores",   Description = "Navegador web rápido con soporte completo de estándares." },
        new() { Id = "zen",             WingetId = "Zen-Team.Zen-Browser",              Name = "Zen Browser",              IconGlyph = "🧘", Category = "Navegadores",   Description = "Navegador minimalista orientado a productividad y bajo consumo." },

        // Compresión
        new() { Id = "7zip",            WingetId = "7zip.7zip",                         Name = "7-Zip",                    IconGlyph = "🗜️", Category = "Compresión",    Description = "Compresor ultraligero multinúcleo de código abierto." },
        new() { Id = "nanazip",         WingetId = "M2Team.NanaZip",                    Name = "NanaZip",                  IconGlyph = "📦", Category = "Compresión",    Description = "Fork moderno de 7-Zip con integración en menú contextual de Windows 11." },
        new() { Id = "winrar",          WingetId = "RARLab.WinRAR",                     Name = "WinRAR",                   IconGlyph = "📚", Category = "Compresión",    Description = "Gestor clásico de archivos comprimidos RAR y ZIP." },

        // Juegos
        new() { Id = "steam",           WingetId = "Valve.Steam",                       Name = "Steam",                    IconGlyph = "🕹️", Category = "Juegos",        Description = "Plataforma principal de juegos para PC." },
        new() { Id = "discord",         WingetId = "Discord.Discord",                   Name = "Discord",                  IconGlyph = "💬", Category = "Juegos",        Description = "Comunicación por voz de baja latencia y comunidades." },
        new() { Id = "epic",            WingetId = "EpicGames.EpicGamesLauncher",       Name = "Epic Games Launcher",      IconGlyph = "🎯", Category = "Juegos",        Description = "Cliente para títulos Unreal Engine y catálogo Epic." },
        new() { Id = "ddu",             WingetId = "Wagnardsoft.DisplayDriverUninstaller", Name = "DDU (Driver Uninstaller)", IconGlyph = "🧹", Category = "Juegos",     Description = "Limpieza total de drivers NVIDIA, AMD e Intel sin residuos." },

        // Utilidades
        new() { Id = "systeminformer",  WingetId = "WinsiderSS.SystemInformer",         Name = "System Informer",          IconGlyph = "⚡", Category = "Utilidades",    Description = "Monitor avanzado de procesos, handles, hilos y servicios de kernel." },
        new() { Id = "quickcpu",        WingetId = "CoderBag.QuickCPU",                 Name = "QuickCPU",                 IconGlyph = "🚀", Category = "Utilidades",    Description = "Ajuste de Core Parking, Turbo Boost y C-States de CPU." },
        new() { Id = "revouninstaller", WingetId = "RevoUninstaller.RevoUninstaller",   Name = "Revo Uninstaller",         IconGlyph = "🧽", Category = "Utilidades",    Description = "Desinstalador con escaneo de claves y carpetas residuales." }
    ];

    public async Task RefreshInstallationStatesAsync(IEnumerable<ToolboxAppItem> items, CancellationToken cancellationToken = default)
    {
        string installedOutput = await CaptureWingetListOutputAsync(cancellationToken).ConfigureAwait(false);
        foreach (var item in items)
        {
            item.IsInstalled = installedOutput.Contains(item.WingetId, StringComparison.OrdinalIgnoreCase);
        }
    }

    public async Task<bool> InstallOrLaunchAsync(ToolboxAppItem item, string provider = "Winget", CancellationToken cancellationToken = default)
    {
        item.IsWorking = true;
        try
        {
            ProcessStartInfo psi = provider.Equals("Chocolatey", StringComparison.OrdinalIgnoreCase)
                ? new ProcessStartInfo
                {
                    FileName = "choco.exe",
                    Arguments = $"install {item.Id} -y",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
                : new ProcessStartInfo
                {
                    FileName = "winget.exe",
                    Arguments = $"install --id \"{item.WingetId}\" -e --silent --accept-package-agreements --accept-source-agreements",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

            using var proc = Process.Start(psi);
            if (proc is null) return false;

            await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (proc.ExitCode == 0)
            {
                item.IsInstalled = true;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            item.IsWorking = false;
        }
    }

    public async Task<bool> UpgradeAllInstalledAppsAsync(string provider = "Winget", CancellationToken cancellationToken = default)
    {
        try
        {
            ProcessStartInfo psi = provider.Equals("Chocolatey", StringComparison.OrdinalIgnoreCase)
                ? new ProcessStartInfo
                {
                    FileName = "choco.exe",
                    Arguments = "upgrade all -y",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
                : new ProcessStartInfo
                {
                    FileName = "winget.exe",
                    Arguments = "upgrade --all --silent --accept-package-agreements --accept-source-agreements",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

            using var proc = Process.Start(psi);
            if (proc is null) return false;
            await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> CaptureWingetListOutputAsync(CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winget.exe",
                Arguments = "list --accept-source-agreements",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(psi);
            if (proc is null) return string.Empty;

            string output = await proc.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            return output;
        }
        catch
        {
            return string.Empty;
        }
    }
}
