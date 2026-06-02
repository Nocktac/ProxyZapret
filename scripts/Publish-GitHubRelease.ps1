param(
    [Parameter(Mandatory)][string] $Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$release = Join-Path $root 'release'
$repo = 'Nocktac/ProxyZapret'
$baseUrl = "https://github.com/$repo/releases/latest/download"

& (Join-Path $PSScriptRoot 'Build-Installer.ps1') -Version $Version
if ($LASTEXITCODE -ne 0) {
    throw 'Installer build failed.'
}

& (Join-Path $PSScriptRoot 'New-UpdateManifest.ps1') `
    -Version $Version `
    -DownloadUrl "$baseUrl/ProxyZapret.exe" `
    -UpdaterDownloadUrl "$baseUrl/ProxyZapret.Updater.exe"
if ($LASTEXITCODE -ne 0) {
    throw 'Update manifest generation failed.'
}

$assets = @(
    (Join-Path $root 'ProxyZapret.exe'),
    (Join-Path $root 'ProxyZapret.Updater.exe'),
    (Join-Path $release "ProxyZapret-Setup-$Version.exe"),
    (Join-Path $release 'update.json')
)

& gh release create "v$Version" $assets `
    --repo $repo `
    --title "ProxyZapret $Version" `
    --generate-notes
if ($LASTEXITCODE -ne 0) {
    throw 'GitHub release publication failed.'
}

Write-Host "Published: https://github.com/$repo/releases/tag/v$Version"
