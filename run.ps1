# ==============================================================================
# lukkyboost — Lanzador Rápido desde GitHub (CMD / PowerShell)
# Repositorio: https://github.com/Lukkyuu/lukkyboost-optimizador
# ==============================================================================

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$RepoOwner = "Lukkyuu"
$RepoName  = "lukkyboost-optimizador"
$ZipUrl    = "https://raw.githubusercontent.com/$RepoOwner/$RepoName/main/dist/lukkyboost.zip"

Write-Host ""
Write-Host "  ======================================================" -ForegroundColor Cyan
Write-Host "   lukkyboost — Windows 10 & 11 Optimization Suite" -ForegroundColor White
Write-Host "   https://github.com/Lukkyuu/lukkyboost-optimizador" -ForegroundColor DarkGray
Write-Host "  ======================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar .NET 8 Desktop Runtime (instalar vía winget si no existe)
$hasDotNet8Desktop = $false
if (Get-Command "dotnet" -ErrorAction SilentlyContinue) {
    $runtimes = & dotnet --list-runtimes 2>$null
    if ($runtimes -match "Microsoft\.WindowsDesktop\.App 8\.") {
        $hasDotNet8Desktop = $true
    }
} elseif (Test-Path "C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\8.*") {
    $hasDotNet8Desktop = $true
}

if (-not $hasDotNet8Desktop) {
    Write-Host "[1/3] Instalando Microsoft .NET 8 Desktop Runtime..." -ForegroundColor Yellow
    if (Get-Command "winget" -ErrorAction SilentlyContinue) {
        & winget install --id Microsoft.DotNet.DesktopRuntime.8 --exact --silent --accept-package-agreements --accept-source-agreements
    }
} else {
    Write-Host "[1/3] .NET 8 Desktop Runtime verificado." -ForegroundColor Green
}

# 2. Preparar directorio en LocalAppData (evita falsos positivos de Defender en %TEMP%)
$appDir  = Join-Path $env:LOCALAPPDATA "LukkyBoost\App"
$zipPath = Join-Path $env:LOCALAPPDATA "LukkyBoost\lukkyboost.zip"

Get-Process -Name "lukkyboost" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
if (Test-Path $appDir) {
    Remove-Item $appDir -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Force -Path $appDir | Out-Null

# 3. Descargar y extraer paquete limpio (sin drivers WinRing0 ni auto-extractores)
Write-Host "[2/3] Descargando lukkyboost desde GitHub..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $ZipUrl -OutFile $zipPath -UseBasicParsing
Expand-Archive -Path $zipPath -DestinationPath $appDir -Force
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

# Limpiar marca Zone.Identifier (SmartScreen) de los archivos extraídos
Get-ChildItem -Path $appDir -Recurse -File | Unblock-File -ErrorAction SilentlyContinue

# 4. Iniciar lukkyboost (con soporte de elevación UAC o ejecución directa si UAC está restringido)
$exePath = Join-Path $appDir "lukkyboost.exe"
if (Test-Path $exePath) {
    Write-Host "[3/3] Iniciando lukkyboost..." -ForegroundColor Green
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if ($isAdmin) {
        Start-Process -FilePath $exePath -WorkingDirectory $appDir
    } else {
        try {
            Start-Process -FilePath $exePath -WorkingDirectory $appDir -Verb RunAs -ErrorAction Stop
        } catch {
            Write-Host "[Aviso] Iniciando en modo estándar..." -ForegroundColor Yellow
            Start-Process -FilePath $exePath -WorkingDirectory $appDir
        }
    }
} else {
    Write-Host "[ERROR] No se encontró lukkyboost.exe tras extraer el paquete." -ForegroundColor Red
}
