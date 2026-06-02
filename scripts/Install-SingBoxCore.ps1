param(
    [string] $Version = '1.13.12'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$runtime = Join-Path $root 'runtime\core-install'
$coreDirectory = Join-Path $root 'core'
$archive = Join-Path $runtime "sing-box-$Version-windows-amd64.zip"
$extractDirectory = Join-Path $runtime 'extract'
$assetName = "sing-box-$Version-windows-amd64.zip"

New-Item -ItemType Directory -Path $runtime -Force | Out-Null
New-Item -ItemType Directory -Path $coreDirectory -Force | Out-Null

$release = Invoke-RestMethod `
    -Uri "https://api.github.com/repos/SagerNet/sing-box/releases/tags/v$Version" `
    -Headers @{ 'User-Agent' = 'ProxyZapret-Core-Installer' }
$asset = @($release.assets | Where-Object name -EQ $assetName)
if ($asset.Count -ne 1) {
    throw "Official release asset was not found: $assetName"
}

Invoke-WebRequest -Uri $asset[0].browser_download_url -OutFile $archive -UseBasicParsing
if ($asset[0].digest -and $asset[0].digest.StartsWith('sha256:')) {
    $expectedHash = $asset[0].digest.Substring(7)
    $actualHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash.ToLowerInvariant()) {
        throw 'Downloaded Sing-box archive hash does not match the official release metadata.'
    }
}

if (Test-Path $extractDirectory) {
    $resolvedRuntime = [IO.Path]::GetFullPath($runtime)
    $resolvedExtract = [IO.Path]::GetFullPath($extractDirectory)
    if (-not $resolvedExtract.StartsWith($resolvedRuntime, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to clean an extraction directory outside runtime.'
    }
    Remove-Item -LiteralPath $resolvedExtract -Recurse -Force
}

Expand-Archive -LiteralPath $archive -DestinationPath $extractDirectory
$binary = @(Get-ChildItem -LiteralPath $extractDirectory -Filter 'sing-box.exe' -File -Recurse)
if ($binary.Count -ne 1) {
    throw 'Downloaded archive does not contain exactly one sing-box.exe binary.'
}

Copy-Item -LiteralPath $binary[0].FullName -Destination (Join-Path $coreDirectory 'sing-box.exe') -Force
& (Join-Path $coreDirectory 'sing-box.exe') version

