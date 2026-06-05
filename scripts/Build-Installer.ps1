param(
    [string] $Version = '0.5.0'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$installer = Join-Path $root 'installer\ProxyZapret.iss'
$productionSettings = Join-Path $root 'config\settings.production.json'
$productionSettingsExample = Join-Path $root 'config\settings.production.example.json'
$compilerCandidates = @(
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe',
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
)
$compiler = $compilerCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $compiler) {
    throw 'Inno Setup 6 compiler is missing. Install JRSoftware.InnoSetup with winget.'
}
if (-not (Test-Path $productionSettings)) {
    Copy-Item -LiteralPath $productionSettingsExample -Destination $productionSettings
    throw "Created $productionSettings. Configure subscriptionUrl before building the installer."
}

& (Join-Path $PSScriptRoot 'Build-ProxyZapret.ps1')
if ($LASTEXITCODE -ne 0) {
    throw 'Application build failed.'
}

& $compiler "/DMyAppVersion=$Version" $installer
if ($LASTEXITCODE -ne 0) {
    throw 'Installer build failed.'
}
