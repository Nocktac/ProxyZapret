package ru.nocktac.proxyzapret.vpn;

import android.os.ParcelFileDescriptor;

import java.io.File;

public final class SingBoxCoreBridge {
    public void start(File configFile, ParcelFileDescriptor tun) {
        throw new UnsupportedOperationException(
            "Android sing-box core bridge is not bundled yet. Integrate official libbox before enabling VPN traffic."
        );
    }

    public void stop() {
    }
}
