param(
    [string] $Version = '0.5.0'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$release = Join-Path $root 'release'
$stage = Join-Path $release "ProxyZapret-Portable-$Version"
$archive = Join-Path $release "ProxyZapret-Portable-$Version.zip"
$productionSettings = Join-Path $root 'config\settings.production.json'
$productionSettingsExample = Join-Path $root 'config\settings.production.example.json'

if (-not (Test-Path $productionSettings)) {
    Copy-Item -LiteralPath $productionSettingsExample -Destination $productionSettings
    throw "Created $productionSettings. Configure subscriptionUrl before building portable package."
}

& (Join-Path $PSScriptRoot 'Build-ProxyZapret.ps1')
if ($LASTEXITCODE -ne 0) {
    throw 'Application build failed.'
}

if (Test-Path $stage) {
    Remove-Item -LiteralPath $stage -Recurse -Force
}
New-Item -ItemType Directory -Path $stage, (Join-Path $stage 'core'), (Join-Path $stage 'config') -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $root 'ProxyZapret.exe') -Destination $stage
Copy-Item -LiteralPath (Join-Path $root 'ProxyZapret.Updater.exe') -Destination $stage
Copy-Item -LiteralPath (Join-Path $root 'assets\ProxyZapret.ico') -Destination (Join-Path $stage "ProxyZapret-$Version.ico")
Copy-Item -LiteralPath (Join-Path $root 'core\sing-box.exe') -Destination (Join-Path $stage 'core')
Copy-Item -LiteralPath (Join-Path $root 'config\routing-rules.json') -Destination (Join-Path $stage 'config')
Copy-Item -LiteralPath (Join-Path $root 'config\settings.example.json') -Destination (Join-Path $stage 'config')
Copy-Item -LiteralPath $productionSettings -Destination (Join-Path $stage 'config\settings.local.json')

$readme = @"
ProxyZapret Portable $Version

Run ProxyZapret.exe as administrator.
This portable build stores settings, subscription cache and runtime files in this folder.
Do not delete the config or core folders.
"@
Set-Content -LiteralPath (Join-Path $stage 'README.txt') -Value $readme -Encoding UTF8

if (Test-Path $archive) {
    Remove-Item -LiteralPath $archive -Force
}
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive -CompressionLevel Optimal

Write-Host "Built: $archive"
