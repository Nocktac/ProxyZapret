$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $root 'ProxyZapret.exe'
$updaterOutput = Join-Path $root 'ProxyZapret.Updater.exe'
$icon = Join-Path $root 'assets\ProxyZapret.ico'

if (-not (Test-Path $compiler)) {
    throw "C# compiler is missing: $compiler"
}
if (-not (Test-Path $icon)) {
    & (Join-Path $PSScriptRoot 'New-BrandAssets.ps1')
}

& $compiler `
    /nologo `
    /target:winexe `
    /optimize+ `
    /platform:x64 `
    "/win32icon:$icon" `
    "/win32manifest:$root\app\ProxyZapret.exe.manifest" `
    "/reference:System.dll" `
    "/reference:System.Core.dll" `
    "/reference:System.Drawing.dll" `
    "/reference:System.Web.Extensions.dll" `
    "/reference:System.Windows.Forms.dll" `
    "/out:$output" `
    "$root\app\Program.cs"

if ($LASTEXITCODE -ne 0) {
    throw 'ProxyZapret.exe build failed.'
}

& $compiler `
    /nologo `
    /target:winexe `
    /optimize+ `
    /platform:x64 `
    "/win32icon:$icon" `
    "/win32manifest:$root\app\ProxyZapret.Updater.exe.manifest" `
    "/reference:System.dll" `
    "/out:$updaterOutput" `
    "$root\app\Updater.cs"

if ($LASTEXITCODE -ne 0) {
    throw 'ProxyZapret.Updater.exe build failed.'
}

Write-Host "Built: $output"
Write-Host "Built: $updaterOutput"
