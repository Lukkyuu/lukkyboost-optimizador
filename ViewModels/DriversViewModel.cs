using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.ViewModels;

public sealed partial class DriversViewModel : ObservableObject
{
    private readonly IGpuDriverService _gpuDriverService;

    [ObservableProperty]
    private string _gpuVendor = "Detectando...";

    [ObservableProperty]
    private string _gpuModelName = "Analizando adaptador PCIe...";

    [ObservableProperty]
    private string _installedDriverVersion = "--";

    [ObservableProperty]
    private string _wmiRawVersion = "--";

    [ObservableProperty]
    private string _officialDriverUrl = "https://www.nvidia.com/Download/index.aspx";

    [ObservableProperty]
    private double _nvidiaShaderCacheMb;

    [ObservableProperty]
    private double _amdAndD3DShaderCacheMb;

    [ObservableProperty]
    private bool _windowsUpdateDriverBlocked;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _operationStatusMessage = "Subsistema gráfico listo para diagnóstico o purga de shaders.";

    public DriversViewModel(IGpuDriverService gpuDriverService)
    {
        _gpuDriverService = gpuDriverService;
    }

    [RelayCommand]
    public async Task RefreshGpuDataAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var info = await _gpuDriverService.DetectActiveGpuAndShadersAsync();
            GpuVendor = info.Vendor;
            GpuModelName = info.ModelName;
            InstalledDriverVersion = info.InstalledDriverVersion;
            WmiRawVersion = info.WmiRawVersion;
            OfficialDriverUrl = info.OfficialDriverUrl;
            NvidiaShaderCacheMb = info.NvidiaShaderCacheSizeMb;
            AmdAndD3DShaderCacheMb = info.AmdAndD3DShaderCacheSizeMb;
            WindowsUpdateDriverBlocked = info.WindowsUpdateDriverBlocked;

            OperationStatusMessage = $"GPU detectada: {info.ModelName} ({info.InstalledDriverVersion}).";
        }
        catch (Exception ex)
        {
            OperationStatusMessage = $"Error al inspeccionar driver de video: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PurgeNvidiaShadersAsync()
    {
        await ExecuteShaderPurgeInternalAsync(ShaderCacheTarget.NvidiaDxAndGl, "NVIDIA (DXCache / GLCache)");
    }

    [RelayCommand]
    private async Task PurgeAmdAndD3DShadersAsync()
    {
        await ExecuteShaderPurgeInternalAsync(ShaderCacheTarget.AmdAndD3D, "AMD / DirectX (D3DSCache / VkCache)");
    }

    [RelayCommand]
    private async Task PurgeAllShadersAsync()
    {
        await ExecuteShaderPurgeInternalAsync(ShaderCacheTarget.AllCaches, "Global (NVIDIA + AMD + D3DSCache)");
    }

    private async Task ExecuteShaderPurgeInternalAsync(ShaderCacheTarget target, string label)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            OperationStatusMessage = $"Purgando caché de compilación de shaders {label}...";
            var result = await _gpuDriverService.PurgeShaderCacheAsync(target);

            // Actualizar tamaños en vivo tras la purga
            var updatedInfo = await _gpuDriverService.DetectActiveGpuAndShadersAsync();
            NvidiaShaderCacheMb = updatedInfo.NvidiaShaderCacheSizeMb;
            AmdAndD3DShaderCacheMb = updatedInfo.AmdAndD3DShaderCacheSizeMb;

            OperationStatusMessage =
                $"Purga {label} completada: {result.MegabytesFreed} MB liberados ({result.FilesDeleted} archivos eliminados, {result.LockedFilesSkipped} en uso por DWM ignorados).";
        }
        catch (Exception ex)
        {
            OperationStatusMessage = $"Excepción controlada durante purga de shaders: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestartVideoSubsystemAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            OperationStatusMessage = "Despachando secuencia Win + Ctrl + Shift + B mediante Win32 SendInput...";
            bool ok = await _gpuDriverService.RestartVideoSubsystemHotAsync();

            OperationStatusMessage = ok
                ? "Subsistema de video y DWM reiniciados en caliente con éxito."
                : "No se pudo enviar la señal de reinicio al controlador gráfico.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleWuDriverBlockAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            bool nextState = !WindowsUpdateDriverBlocked;
            bool ok = await _gpuDriverService.SetWindowsUpdateDriverBlockAsync(nextState);
            if (ok)
            {
                WindowsUpdateDriverBlocked = nextState;
                OperationStatusMessage = nextState
                    ? "Búsqueda automática de drivers en Windows Update BLOQUEADA (recomendado para instalación limpia)."
                    : "Búsqueda automática de drivers en Windows Update RESTAURADA.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenVendorDriverUrl()
    {
        if (string.IsNullOrWhiteSpace(OfficialDriverUrl)) return;

        try
        {
            Process.Start(new ProcessStartInfo(OfficialDriverUrl) { UseShellExecute = true });
            OperationStatusMessage = $"Abriendo portal oficial de descargas limpias de {GpuVendor}...";
        }
        catch (Exception ex)
        {
            OperationStatusMessage = $"No se pudo abrir el navegador: {ex.Message}";
        }
    }
}

