# ProxyZapret

Minimal Windows `.exe` split-tunnel client for a managed Remnawave subscription.
An Android foundation is also available under `android/`.

The end-user interface intentionally exposes one action only: turn the proxy on
or off. Server credentials come from Remnawave. Routing rules are shipped with
the application and cannot be edited from the UI.

## Current foundation

- Windows tray application compiled as `ProxyZapret.exe`.
- Remnawave subscription fetch with a stable random `x-hwid` header.
- Extensible standard Base64 URI import for Hysteria2, VLESS with Reality or
  TLS, Trojan, and Shadowsocks. Supported URI nodes are preferred in that order,
  with automatic fallback to Sing-box JSON outbounds.
- Generated Sing-box TUN configuration with automatic node health checks.
- DNS hijacking inside the TUN adapter with direct Cloudflare DNS-over-HTTPS.
- TCP and UDP support for managed Hysteria2 and Shadowsocks nodes.
- Built-in domain, IP CIDR, and Windows process routing rules.
- Complete Russia blocked-domain and blocked-IP rule sets from
  `runetfreedom/russia-v2ray-rules-dat`, refreshed every 6 hours.
- Last-known-good subscription cache.
- Background subscription refresh while the tunnel is running.
- Core configuration validation before startup.
- Startup application update check with SHA-256 validation and automatic restart.

## Run locally

1. Install the pinned official Sing-box core:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Install-SingBoxCore.ps1
   ```

2. Copy `config\settings.example.json` to `config\settings.local.json`.
3. Set `subscriptionUrl` in the local file.
4. Build the application:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-ProxyZapret.ps1
   ```

5. Run `ProxyZapret.exe`.

TUN mode needs administrator rights. `ProxyZapret.exe` requests elevation
through its Windows application manifest.

The local settings file is ignored by design and must not be distributed. When
Remnawave access is connected, the URL can instead be delivered by an activation
flow or by a branded build pipeline.

The managed blocklists are downloaded by Sing-box from the JSDelivr mirror of
`runetfreedom/russia-v2ray-rules-dat` and cached in `runtime\rules-cache.db`.
The application uses `geosite-ru-blocked-all.srs` for the full known domain set
and `geoip-ru-blocked.srs` for blocked IP ranges. It also includes maintained
service rule sets for Discord, Telegram, Meta, Instagram, YouTube, Roblox,
Twitter, TikTok, and WhatsApp. Built-in service rules are evaluated first so
common apps remain covered before the first list refresh.

## Application updates

`ProxyZapret.exe` checks `updateManifestUrl` from `config\settings.local.json`
when the user opens the application. If the manifest contains a newer version,
the app downloads the new binary, validates its SHA-256 hash, updates
`ProxyZapret.Updater.exe` when needed, and starts the updater. The updater waits
for the running app to exit, preserves `ProxyZapret.exe.previous`, replaces the
executable, and starts the new version automatically.

The public update endpoint should serve JSON such as:

```json
{
  "version": "0.4.1",
  "url": "https://downloads.example.com/ProxyZapret.exe",
  "sha256": "...",
  "updaterUrl": "https://downloads.example.com/ProxyZapret.Updater.exe",
  "updaterSha256": "..."
}
```

Generate it after building a release:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\New-UpdateManifest.ps1 `
  -Version 0.4.1 `
  -DownloadUrl https://downloads.example.com/ProxyZapret.exe `
  -UpdaterDownloadUrl https://downloads.example.com/ProxyZapret.Updater.exe
```

Publish `ProxyZapret.exe`, `ProxyZapret.Updater.exe`, and `release\update.json`.
If the update server is unavailable or the hash does not match, the installed
application continues to open normally.

The in-app updater replaces `ProxyZapret.exe`. Publish a new installer release
when changing the Sing-box core or bundled config files.

For the configured GitHub repository, publish a release with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Publish-GitHubRelease.ps1 `
  -Version 0.4.1
```

## Development checks

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-ProxyZapret.ps1
.\ProxyZapret.exe --self-test
.\ProxyZapret.exe --subscription-test
.\ProxyZapret.exe --tunnel-test
.\ProxyZapret.exe --udp-proxy-test
.\ProxyZapret.exe --smoke-test
```

The self-test validates the embedded rules, generates a config from the sample
subscription, and asks the real Sing-box core to parse it. The smoke test starts
and automatically closes the Windows GUI. The subscription test fetches the
locally configured Remnawave subscription and validates its two managed nodes
without starting the tunnel. The tunnel test briefly starts and automatically
stops the full TUN stack.
The UDP proxy test sends a DNS probe through each selected managed node.

## Windows installer

The installer deploys the application to `C:\Program Files\ProxyZapret`, keeps
writable state under `%ProgramData%\ProxyZapret`, and creates desktop and Start
Menu shortcuts.

1. Copy `config\settings.production.example.json` to
   `config\settings.production.json`.
2. Configure the managed subscription URL and update manifest URL.
3. Build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Installer.ps1
```

The result is written to `release\ProxyZapret-Setup-0.4.0.exe`.

## Android client

The Android project lives in `android/`. It includes a native one-button UI,
`VpnService` lifecycle, Remnawave subscription loading, URI parsing, and
sing-box JSON generation.

Android traffic forwarding is intentionally fail-closed until the official
sing-box/libbox Android bridge is integrated. See `android\README.md`.
