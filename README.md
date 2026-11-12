<div align="center">
  <img src="Assets/logo.png" alt="lukkyboost logo" width="140" height="140" />
  <h1>lukkyboost</h1>
  <p><strong>Suite avanzada de optimización de bajo nivel, debloat profundo, telemetría en tiempo real y gestión de perfiles para Windows 10 & 11</strong></p>

  ![.NET 8](https://img.shields.io/badge/.NET%208.0-WPF%20x64-512BD4?style=for-the-badge&logo=dotnet)
  ![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011%20(x64)-0078D6?style=for-the-badge&logo=windows)
  ![Architecture](https://img.shields.io/badge/Arquitectura-MVVM%20(CommunityToolkit)-38BDF8?style=for-the-badge)
  ![Privileges](https://img.shields.io/badge/UAC-Administrator%20Required-10B981?style=for-the-badge)
</div>

---

## ⚡ Descripción General

**lukkyboost** es una herramienta de escritorio nativa desarrollada en **C# / .NET 8 (WPF)** bajo una estética **Deep Cyber Navy** (`#0A0F1D` / `#111827` / `#38BDF8`), diseñada para maximizar el rendimiento del sistema operativo, reducir la latencia de red y entrada (*input lag*), eliminar componentes innecesarios (*bloatware*) y monitorear sensores de hardware en tiempo real sin comprometer la estabilidad del sistema.

---

## 🧩 Módulos y Características Principales

### 1. 💻 General (Perfiles Rápidos & Telemetría en Tiempo Real)
- **Selector de Perfiles en 1 Clic:**
  - 🎮 **Juegos:** Activa el plan de energía *Ultimate Performance*, deshabilita el algoritmo de Nagle (`TCPNoDelay = 1`, `TcpAckFrequency = 1`), reduce `NetworkThrottlingIndex` y detiene servicios de indexación/telemetría en segundo plano.
  - 💻 **Código:** Optimizado para desarrollo de software; preserva intactos los subsistemas de virtualización (`WSL2`, `Hyper-V`, `Docker`, contenedores y motores de bases de datos) mientras prioriza rendimiento de CPU/RAM.
  - 📑 **Oficina:** Equilibrio energético y acústico ideal para sesiones de trabajo prolongadas.
  - ⚖️ **Normal:** Restaura los parámetros estándar de fábrica de Windows.
- **Dashboard de Sensores 2x2 (1000 ms):**
  - **CPU:** Modelo exacto, frecuencia actual (MHz), temperatura (°C) y curva de carga en tiempo real (`LiveChartsCore` + `LibreHardwareMonitorLib`).
  - **GPU:** Modelo detectado (NVIDIA / AMD / Intel), versión pública de driver WHQL, VRAM en uso/total, temperatura y curva de carga.
  - **RAM:** Consumo en GB, capacidad total instalada, velocidad en MHz y curva de utilización.
  - **Almacenamiento:** Modelo de unidad principal, espacio libre/total en GB y tasa de transferencia en MB/s.

### 2. 🧹 Debloat
- Desactivación de **Windows Copilot**, **Windows Recall** y análisis de datos de IA.
- Eliminación de aplicaciones preinstaladas de Microsoft y *bloatware* promocionado de terceros (Spotify, Clipchamp, TikTok, etc.).
- Desinstalación limpia de **OneDrive** y remoción de su acceso en el Explorador.
- Optimización de políticas para **Microsoft Edge** y **Brave** (desactivación de ejecución en segundo plano, telemetría, barra lateral, criptobilleteras y recompensas).
- Desactivación de **Windows Widgets** (`News and Interests` / `WebExperience`).

### 3. 🛡️ Privacidad
- Revocación global de permisos de aplicaciones en segundo plano (ubicación, cámara, micrófono, contactos).
- Extensión del límite de pausa de **Windows Update hasta 20 años** (`7300 días`).
- Desactivación de la tabla binaria de plataforma **WPBT** (previene inyección de ejecutables OEM desde la BIOS/UEFI).
- Bloqueo de cifrado automático de **BitLocker**, sincronización en la nube, historial de actividad (*Timeline*) y eliminación del usuario residual `defaultuser0`.

### 4. 📡 Telemetría
- Bloqueo de telemetría nativa de Windows (`DiagTrack`, `dmwappushservice`, `WerSvc`, `AllowTelemetry = 0`).
- Desactivación de telemetría de terceros (Microsoft Office / contenedores de telemetría de NVIDIA).
- Desactivación de optimización de distribución P2P de Windows Update (`DoSvc`).

### 5. 🚀 Rendimiento
- Configuración de servicios secundarios en inicio **Manual** (`demand`) para liberar memoria sin romper dependencias.
- Control de **HAGS** (*Hardware-Accelerated GPU Scheduling*) y **VBS / Aislamiento de Núcleo** (`hypervisorlaunchtype`).
- Priorización de **IPv4 sobre IPv6** y desactivación de *Nagle's Algorithm* por adaptador NIC activo.
- Reducción de `MenuShowDelay` a `0 ms`, límite de uso de CPU de Windows Defender al `25%`, y control de `SysMain`, `WSearch` y *Fast Startup*.

### 6. 🎨 Varios (Interfaz & Kernel)
- Restauración del **Menú Contextual Clásico de Windows 10** en Windows 11.
- Desactivación de **MPO (Multiplane Overlay)** en DWM para solucionar parpadeos y *stuttering* en GPUs modernas.
- Opción **"Finalizar tarea"** directo desde la barra de tareas, ocultación de *Inicio* y *Galería* en el Explorador, visualización de extensiones de archivo y pantalla azul (BSOD) detallada.

### 7. 📦 Explorar Aplicaciones (Winget Toolbox)
- Catálogo integrado con detección de estado instalado e instalación/desinstalación silenciosa vía `winget` (Brave, Firefox, 7-Zip, Discord, Steam, VLC, Git, VS Code, Lively Wallpaper, Malwarebytes).

### 8. 🎮 Drivers & Caché de Shaders
- Identificación por WMI de la GPU principal y enlace directo a los drivers oficiales WHQL.
- **Purgado seguro de Caché de Shaders:** Limpieza de `DXCache`, `GLCache`, `ComputeCache`, `NV_Cache` (NVIDIA) y `DxCache`, `D3DSCache`, `VkCache` (AMD/DirectX), ignorando archivos bloqueados por DWM.
- **Reinicio de Video en Caliente (`Win + Ctrl + Shift + B`):** Reinicia el subsistema gráfico mediante la API nativa `SendInput` (`user32.dll`) sin reiniciar la PC.
- Bloqueo de actualizaciones automáticas de drivers por Windows Update.

### 9. 💾 Copias de Seguridad y Rollback
- Creación de **Puntos de Restauración de Windows** vía WMI (`SystemRestore.CreateRestorePoint`).
- Exportación fechada de ramas de registro (`.reg`) y estado previo de servicios/red en archivos JSON (`%LocalAppData%\LukkyBoost\Backups`).
- Restauración completa del estado previo en un solo clic.

---

## 🛠️ Requisitos Técnicos

- **Sistema Operativo:** Windows 10 (19041+) o Windows 11 (64 bits).
- **Permisos:** Ejecución como **Administrador** (forzado automáticamente mediante `app.manifest`).
- **SDK (solo para compilar desde código fuente):** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

---

## 📦 Compilar Ejecutable Único Portable (`.exe`)

Para generar un único archivo `lukkyboost.exe` autocontenido (que no requiere tener .NET instalado en otras PCs):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o ./publish
```

El ejecutable final quedará listo en `./publish/lukkyboost.exe`.

---

## 📂 Arquitectura del Proyecto

```text
lukkyboost/
├── Assets/                  # Logotipo oficial (logo.png) e icono multirresolución (app.ico)
├── Core/
│   ├── Enums/               # Enumeraciones de pestañas y perfiles de rendimiento
│   ├── Interfaces/          # Contratos de servicios para Inyección de Dependencias
│   ├── Models/              # Modelos inmutables y observables (TweakToggleItem, TelemetryMetrics, etc.)
│   └── Win32/               # P/Invoke nativo (user32.dll SendInput para reinicio de driver de video)
├── Services/                # Lógica de bajo nivel (WMI, Registro, Servicios, Winget, Sensores, Backups)
├── ViewModels/              # Lógica de presentación MVVM (CommunityToolkit.Mvvm)
├── Views/                   # Ventanas WPF (SplashScreenView, MainWindow) y páginas modulares
├── app.manifest             # Manifiesto UAC (requireAdministrator)
└── lukkyboost.csproj        # Configuración de compilación .NET 8 WPF x64
```

