using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LukkyBoost.App.Core.Interfaces;
using LukkyBoost.App.Core.Models;

namespace LukkyBoost.App.ViewModels;

public sealed partial class ConfigViewModel : ObservableObject
{
    private readonly ISystemOptimizerService _optimizerService;
    private readonly List<TweakToggleItem> _allTweaks = [];

    [ObservableProperty]
    private string _categoryHeader = "Debloat";

    [ObservableProperty]
    private string _statusMessage = "Listo.";

    public ObservableCollection<TweakToggleItem> VisibleTweaks { get; } = [];

    public IReadOnlyList<TweakToggleItem> AllTweaks => _allTweaks;

    public ConfigViewModel(ISystemOptimizerService optimizerService)
    {
        _optimizerService = optimizerService;
        BuildTweaksCatalog();
        ShowCategory("Debloat", "Debloat");
    }

    public void ShowCategory(string categoryKey, string title)
    {
        CategoryHeader = title;
        VisibleTweaks.Clear();
        foreach (var item in _allTweaks.Where(t => t.Category == categoryKey))
        {
            VisibleTweaks.Add(item);
        }
    }

    public void UncheckAllTweaks()
    {
        foreach (var item in _allTweaks)
        {
            item.SetStateSilently(false);
        }

        StatusMessage = "Todas las opciones han sido desmarcadas.";
    }

    public IReadOnlyList<string> GetEnabledTweakIds() =>
        _allTweaks.Where(t => t.IsActive).Select(t => t.Id).ToList();

    public async Task<int> ApplyAllSelectedTweaksAsync(
        IProgress<ProgressReport>? progress = null,
        double startPercent = 35,
        double endPercent = 90,
        CancellationToken ct = default)
    {
        var activeItems = _allTweaks.Where(t => t.IsActive).ToList();
        if (activeItems.Count == 0)
        {
            progress?.Report(new ProgressReport
            {
                Percentage = endPercent,
                StepMessage = "Verificando configuración activa...",
                CompletedLog = "✔ Sin cambios adicionales pendientes en módulos"
            });
            return 0;
        }

        int appliedCount = 0;
        for (int i = 0; i < activeItems.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var item = activeItems[i];
            double pct = startPercent + ((i + 1.0) / activeItems.Count) * (endPercent - startPercent);

            progress?.Report(new ProgressReport
            {
                Percentage = pct,
                StepMessage = $"{item.Title}..."
            });

            if (await _optimizerService.ApplyTweakByIdAsync(item.Id, true, ct).ConfigureAwait(false))
            {
                appliedCount++;
                progress?.Report(new ProgressReport
                {
                    Percentage = pct,
                    StepMessage = $"{item.Title}...",
                    CompletedLog = $"✔ {item.Title}"
                });
            }
        }

        return appliedCount;
    }

    public async Task ApplyImportedTweaksAsync(IEnumerable<string> enabledIds)
    {
        var set = new HashSet<string>(enabledIds, StringComparer.OrdinalIgnoreCase);
        foreach (var item in _allTweaks)
        {
            bool shouldEnable = set.Contains(item.Id);
            if (item.IsActive != shouldEnable)
            {
                item.SetStateSilently(shouldEnable);
                await _optimizerService.ApplyTweakByIdAsync(item.Id, shouldEnable);
            }
        }

        StatusMessage = "Configuración importada y aplicada.";
    }

    private void BuildTweaksCatalog()
    {
        void Add(string id, string cat, string icon, string title, string desc)
        {
            _allTweaks.Add(new TweakToggleItem(
                id,
                cat,
                icon,
                title,
                desc,
                (tweakId, enable) => _optimizerService.ApplyTweakByIdAsync(tweakId, enable),
                msg => StatusMessage = msg));
        }

        // A) DEBLOAT
        Add("debloat.windows_ai",       "Debloat", "🤖", "IA de Windows (Copilot & Recall)", "Bloquea Windows Copilot (TurnOffWindowsCopilot = 1), desactiva Windows Recall (DisableAIDataAnalysis = 1) y oculta iconos de IA.");
        Add("debloat.ms_apps",          "Debloat", "🗑️", "Eliminar aplicaciones de Microsoft", "Desinstala bloatware preinstalado (Solitario, Contactos, Mapas, Feedback Hub, Groove/Media Player).");
        Add("debloat.third_party",      "Debloat", "🚫", "Eliminar aplicaciones de terceros", "Purga apps promocionadas (Spotify, Shazam, Clipchamp) y bloquea SilentInstalledAppsEnabled = 0.");
        Add("debloat.extensions",       "Debloat", "🎞️", "Eliminar extensiones OEM", "Desinstala paquetes de códecs obsoletos preinstalados por fabricantes (HEVC OEM, VP9).");
        Add("debloat.xbox",             "Debloat", "🎮", "Eliminar Xbox", "Desinstala componentes y detiene servicios Xbox para usuarios que no utilizan Game Pass.");
        Add("debloat.ms_store",         "Debloat", "🛍️", "Eliminar / Bloquear Microsoft Store", "Aplica directiva RemoveWindowsStore = 1 para deshabilitar la tienda.");
        Add("debloat.store_autoupdate", "Debloat", "⏸️", "Desactivar actualizaciones automáticas de apps", "Bloquea descargas automáticas en segundo plano de Microsoft Store (AutoDownload = 2).");
        Add("debloat.onedrive",         "Debloat", "☁️", "Eliminar OneDrive", "Cierre forzado y desinstalación vía OneDriveSetup.exe /uninstall eliminando su clave CLSID del Explorador.");
        Add("debloat.edge",             "Debloat", "🌐", "Reducir bloat de Edge", "Desactiva funciones promocionales, compras, StartupBoost y telemetría de Microsoft Edge.");
        Add("debloat.brave",            "Debloat", "🦁", "Reducir bloat de Brave", "Desactiva Brave Rewards, Brave News, VPN y monedero de criptomonedas mediante políticas.");
        Add("debloat.widgets",          "Debloat", "📰", "Eliminar Widgets", "Desinstala Web Experience Pack y oculta el icono de Widgets de la barra de tareas.");

        // B) PRIVACIDAD
        Add("privacy.app_permissions",  "Privacidad", "🔒", "Desactivar permisos de acceso de aplicaciones", "Bloqueo global en segundo plano a ubicación, cámara, micrófono, contactos y documentos.");
        Add("privacy.wu_pause_20y",     "Privacidad", "⏳", "Extender el límite de pausa para Windows Update", "Amplía el límite de pausa de actualizaciones en el registro hasta por 20 años (7300 días).");
        Add("privacy.wpbt",             "Privacidad", "🛡️", "Desactivar WPBT (Windows Platform Binary Table)", "Bloquea la ejecución de binarios inyectados por el fabricante desde la tabla ACPI de la BIOS/UEFI.");
        Add("privacy.bitlocker_auto",   "Privacidad", "🔑", "Desactivar cifrado automático de BitLocker", "Previene el cifrado automático PreventDeviceEncryption = 1 en unidades nuevas.");
        Add("privacy.cloud_sync",       "Privacidad", "🔄", "Desactivar sincronización en la nube", "Desactiva la sincronización de contraseñas, temas y configuración de cuenta Microsoft.");
        Add("privacy.timeline",         "Privacidad", "📜", "Desactivar historial de actividades y Timeline", "Establece EnableActivityFeed = 0, PublishUserActivities = 0 y UploadUserActivities = 0.");
        Add("privacy.notifications",    "Privacidad", "🔕", "Desactivar bandeja de notificaciones y centro de actividades", "Aplica DisableNotificationCenter = 1 en políticas del Explorador.");
        Add("privacy.offline_maps",     "Privacidad", "🗺️", "Desactivar descarga automática de mapas sin conexión", "Detiene el servicio MapsBroker y bloquea el tráfico automático de mapas.");
        Add("privacy.default0_user",    "Privacidad", "👤", "Eliminar usuario 'default0' remanente de OOBE", "Elimina cuentas residuales defaultuser0 / default0 creadas durante la instalación inicial.");
        Add("privacy.lockscreen_cam",   "Privacidad", "📷", "Desactivar cámara en bloqueo y biometría", "Aplica NoLockScreenCamera = 1 y detiene el servicio de biometría WbioSrvc.");

        // C) TELEMETRÍA
        Add("telemetry.windows_core",   "Telemetria", "📡", "Desactivar telemetría de Windows", "Detiene y deshabilita DiagTrack, dmwappushservice y WerSvc; fuerza AllowTelemetry = 0.");
        Add("telemetry.third_party",    "Telemetria", "🕵️", "Desactivar telemetría de terceros", "Bloquea servicios y directivas de seguimiento de telemetría en contenedores NVIDIA y Office.");
        Add("telemetry.windows_update", "Telemetria", "📥", "Administrar / Configurar Windows Update (DoSvc)", "Desactiva Entrega Optimizada P2P (DODownloadMode = 0) y limita descargas en segundo plano.");

        // D) RENDIMIENTO
        Add("perf.manual_services",      "Rendimiento", "⚙️", "Configurar servicios en modo manual", "Cambia servicios de arranque automático prescindibles para que inicien solo bajo demanda.");
        Add("perf.disable_transparency", "Rendimiento", "🪟", "Desactivar transparencia de Windows", "Establece EnableTransparency = 0 para liberar ciclos de composición en GPU y DWM.");
        Add("perf.disable_hags",         "Rendimiento", "🎞️", "Desactivar HAGS (Hardware Accelerated GPU Scheduling)", "Ajusta HwSchMode = 1 para mitigar micro-stuttering en motores específicos.");
        Add("perf.ipv4_tcp_nagle",       "Rendimiento", "⚡", "Establecer IPv4 preferido y optimizar TCP (Nagle Off)", "Inyecta TCPNoDelay = 1, TcpAckFrequency = 1 y NetworkThrottlingIndex = 0xFFFFFFFF.");
        Add("perf.menu_show_delay",      "Rendimiento", "🖱️", "Desactivar retraso del ratón en menús", "Reduce MenuShowDelay a 0 ms para respuesta instantánea en menús contextuales.");
        Add("perf.defender_cpu_limit",   "Rendimiento", "🛡️", "Limitar uso de CPU para Windows Defender", "Fija AvgCPULoadFactor = 25 para evitar caídas de FPS durante escaneos en segundo plano.");
        Add("perf.disable_vbs",          "Rendimiento", "🚀", "Desactivar aislamiento del núcleo (Core Isolation / VBS)", "Ejecuta bcdedit /set hypervisorlaunchtype off para recuperar rendimiento en CPU/GPU.");
        Add("perf.disable_sysmain",      "Rendimiento", "💿", "Desactivar SysMain (Superfetch)", "Detiene y deshabilita SysMain para evitar escrituras innecesarias en unidades NVMe/SSD.");
        Add("perf.disable_search",       "Rendimiento", "🔍", "Desactivar sensor de almacenamiento e indexación Search", "Desactiva Storage Sense y el servicio de indexación WSearch en segundo plano.");
        Add("perf.disable_fast_startup", "Rendimiento", "🔌", "Desactivar Inicio rápido (Fast Startup)", "Fija HiberbootEnabled = 0 para garantizar un arranque limpio de kernel en cada reinicio.");

        // E) VARIOS
        Add("misc.classic_context_menu", "Varios", "📋", "Restaurar menú contextual clásico de Windows 10", "Registra CLSID {86ca1aa0-34aa-4e8b-a509-50c905bae2a2} para abrir el menú completo sin clic adicional.");
        Add("misc.disable_mpo",          "Varios", "🖥️", "Desactivar superposición multiplano (MPO)", "Fija OverlayTestMode = 5 en DWM para eliminar parpadeos y stuttering en GPUs modernas.");
        Add("misc.taskbar_end_task",     "Varios", "✖️", "Agregar 'Finalizar tarea' en la barra de tareas", "Activa TaskbarEndTask = 1 al hacer clic derecho sobre cualquier app en la barra de tareas.");
        Add("misc.remove_home_gallery",  "Varios", "📁", "Eliminar 'Inicio' y 'Galería' del Explorador", "Oculta los nodos CLSID de Inicio y Galería del panel de navegación izquierdo.");
        Add("misc.taskbar_left",         "Varios", "⬅️", "Mover iconos de la barra de tareas a la izquierda", "Establece TaskbarAl = 0 en Explorer\\Advanced.");
        Add("misc.numlock_stickykeys",   "Varios", "⌨️", "Desactivar Bloq Num al iniciar y Sticky Keys", "Configura InitialKeyboardIndicators = 0 y desactiva el atajo de teclas especiales (Flags = 506).");
        Add("misc.force_dark_mode",      "Varios", "🌑", "Forzar modo oscuro en todo el sistema y aplicaciones", "Aplica AppsUseLightTheme = 0 y SystemUsesLightTheme = 0.");
        Add("misc.show_extensions",      "Varios", "📄", "Mostrar extensiones de archivo y carpetas ocultas", "Fija HideFileExt = 0 y Hidden = 1 en el Explorador de Windows.");
        Add("misc.disable_snap_layouts", "Varios", "🔲", "Desactivar diseños de acoplamiento (Snap Layouts)", "Establece EnableSnapAssistFlyout = 0 al pasar el cursor sobre el botón maximizar.");
        Add("misc.detailed_bsod",        "Varios", "📘", "Activar BSOD detallada e inicio de sesión detallado", "Fija DisplayParameters = 1 en CrashControl y VerboseStatus = 1.");
    }
}
