param(
    [string] $Version = '0.5.0',
    [Parameter(Mandatory)][string] $DownloadUrl,
    [Parameter(Mandatory)][string] $UpdaterDownloadUrl,
    [string] $Output = '.\release\update.json'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'ProxyZapret.exe'
$updater = Join-Path $root 'ProxyZapret.Updater.exe'
if (-not (Test-Path $exe)) {
    throw "Application binary is missing: $exe"
}
if (-not (Test-Path $updater)) {
    throw "Updater binary is missing: $updater"
}

$outputPath = [IO.Path]::GetFullPath((Join-Path $root $Output))
New-Item -ItemType Directory -Path (Split-Path -Parent $outputPath) -Force | Out-Null

$manifest = [ordered]@{
    version = $Version
    url = $DownloadUrl
    sha256 = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    updaterUrl = $UpdaterDownloadUrl
    updaterSha256 = (Get-FileHash -LiteralPath $updater -Algorithm SHA256).Hash.ToLowerInvariant()
}
$json = $manifest | ConvertTo-Json
[IO.File]::WriteAllText($outputPath, $json, (New-Object Text.UTF8Encoding($false)))
Write-Host "Generated: $outputPath"
