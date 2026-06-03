# ProxyZapret Android

Android foundation for the managed ProxyZapret client.

## Current state

- Native Android project under `android/`.
- One-button UI matching the Windows client direction.
- `VpnService` foreground-service lifecycle.
- Remnawave subscription fetch with Android `x-hwid`.
- Standard URI parser for Hysteria2, VLESS with TLS or Reality, Trojan, and
  Shadowsocks.
- Sing-box JSON generation with the same managed-node priority as Windows.
- GitHub Actions workflow can build a debug APK.

## Important

The Android core bridge is intentionally not enabled yet. Android needs a real
sing-box/libbox integration connected to `VpnService`; a standalone CLI binary
is not enough for a safe system VPN. The placeholder bridge fails closed so the
app cannot silently create a broken VPN route.

Next implementation step: vendor the official Android sing-box/libbox bridge
and replace `SingBoxCoreBridge` with a real runner.

## Local config

Create `android/local.properties`:

```properties
proxyzapret.subscriptionUrl=https://example.com/sub/...
```

or build with:

```powershell
$env:PROXYZAPRET_SUBSCRIPTION_URL='https://example.com/sub/...'
gradle -p android assembleDebug
```

`local.properties` is ignored by Git.
