package ru.nocktac.proxyzapret.vpn;

import android.os.ParcelFileDescriptor;

import java.io.File;

public final class SingBoxCoreBridge {
    public static boolean isAvailable() {
        return false;
    }

    public static String unavailableReason() {
        return "Android VPN core is not bundled yet. The UI and subscription parser are ready; libbox integration is the next step.";
    }

    public void start(File configFile, ParcelFileDescriptor tun) {
        throw new UnsupportedOperationException(unavailableReason());
    }

    public void stop() {
    }
}
