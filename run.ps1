# ==============================================================================
# lukkyboost — Lanzador Rápido desde GitHub (CMD / PowerShell)
# Repositorio: https://github.com/Lukkyuu/lukkyboost-optimizador
# ==============================================================================

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$RepoOwner  = "Lukkyuu"
$RepoName   = "lukkyboost-optimizador"
$ScriptUrl  = "https://raw.githubusercontent.com/$RepoOwner/$RepoName/main/run.ps1"
$ReleaseUrl = "https://github.com/$RepoOwner/$RepoName/releases/latest/download/lukkyboost.exe"
$ZipUrl     = "https://raw.githubusercontent.com/$RepoOwner/$RepoName/main/dist/lukkyboost.zip"

# 1. Auto-elevación a Administrador (UAC) si se ejecutó desde un CMD sin privilegios
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "[lukkyboost] Solicitando permisos de Administrador (UAC)..." -ForegroundColor Cyan
    Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -Command `"irm '$ScriptUrl' | iex`""
    return
}

Write-Host ""
Write-Host "  ======================================================" -ForegroundColor Cyan
Write-Host "   lukkyboost — Windows 10 & 11 Optimization Suite" -ForegroundColor White
Write-Host "   https://github.com/Lukkyuu/lukkyboost-optimizador" -ForegroundColor DarkGray
Write-Host "  ======================================================" -ForegroundColor Cyan
Write-Host ""

# 2. Verificar .NET 8 Desktop Runtime (instalar vía winget si no existe)
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

# 3. Preparar directorio temporal limpio
$workDir = Join-Path $env:TEMP "LukkyBoost_Runner"
New-Item -ItemType Directory -Force -Path $workDir | Out-Null
$exePath = Join-Path $workDir "lukkyboost.exe"
$zipPath = Join-Path $workDir "lukkyboost.zip"

# Cerrar instancia previa si estuviera abierta
Get-Process -Name "lukkyboost" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# 4. Descargar ejecutable desde GitHub Releases o desde /dist/lukkyboost.zip del repositorio
Write-Host "[2/3] Descargando lukkyboost desde GitHub..." -ForegroundColor Cyan
$downloaded = $false

try {
    Invoke-WebRequest -Uri $ReleaseUrl -OutFile $exePath -UseBasicParsing -ErrorAction Stop
    if ((Test-Path $exePath) -and ((Get-Item $exePath).Length -gt 100000)) {
        $downloaded = $true
    }
} catch {
    $downloaded = $false
}

if (-not $downloaded) {
    Invoke-WebRequest -Uri $ZipUrl -OutFile $zipPath -UseBasicParsing
    Expand-Archive -Path $zipPath -DestinationPath $workDir -Force
    Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
}

# 5. Ejecutar lukkyboost
if (Test-Path $exePath) {
    Write-Host "[3/3] Iniciando lukkyboost..." -ForegroundColor Green
    Start-Process -FilePath $exePath -WorkingDirectory $workDir
} else {
    Write-Host "[ERROR] No se pudo descargar lukkyboost.exe desde el repositorio." -ForegroundColor Red
}
