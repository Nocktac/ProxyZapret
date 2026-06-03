package ru.nocktac.proxyzapret.vpn;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Intent;
import android.net.VpnService;
import android.os.Build;
import android.os.ParcelFileDescriptor;

import org.json.JSONObject;

import java.io.File;

import ru.nocktac.proxyzapret.AppState;
import ru.nocktac.proxyzapret.BuildConfig;
import ru.nocktac.proxyzapret.MainActivity;
import ru.nocktac.proxyzapret.subscription.ConfigBuilder;
import ru.nocktac.proxyzapret.subscription.RemnawaveClient;

public final class ProxyZapretVpnService extends VpnService {
    private static final String CHANNEL_ID = "proxyzapret-vpn";
    private ParcelFileDescriptor tun;
    private SingBoxCoreBridge coreBridge;

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        String action = intent == null ? AppState.ACTION_START : intent.getAction();
        if (AppState.ACTION_STOP.equals(action)) {
            stopVpn("Turn on to connect");
            return START_NOT_STICKY;
        }

        startForeground(1001, notification("Starting ProxyZapret"));
        new Thread(this::startVpn, "ProxyZapretStart").start();
        return START_STICKY;
    }

    @Override
    public void onDestroy() {
        stopVpn("Turn on to connect");
        super.onDestroy();
    }

    private void startVpn() {
        try {
            if (BuildConfig.SUBSCRIPTION_URL == null || BuildConfig.SUBSCRIPTION_URL.trim().isEmpty()) {
                throw new IllegalStateException("Subscription URL is not configured in this Android build.");
            }

            setStatus(false, "Updating managed subscription...");
            RemnawaveClient client = new RemnawaveClient(this, BuildConfig.SUBSCRIPTION_URL);
            ConfigBuilder builder = new ConfigBuilder(this);
            JSONObject config = builder.build(client.load());

            File configFile = new File(getFilesDir(), "sing-box.generated.json");
            builder.save(config, configFile);

            tun = new Builder()
                .setSession("ProxyZapret")
                .addAddress("172.19.0.1", 30)
                .addDnsServer("1.1.1.1")
                .addRoute("0.0.0.0", 0)
                .establish();
            if (tun == null) throw new IllegalStateException("Android VPN permission was not granted.");

            coreBridge = new SingBoxCoreBridge();
            coreBridge.start(configFile, tun);
            setStatus(true, "Restricted services use the secure route");
            startForeground(1001, notification("ProxyZapret is connected"));
        } catch (Exception exception) {
            setStatus(false, exception.getMessage());
            stopVpn(exception.getMessage());
        }
    }

    private Notification notification(String text) {
        NotificationManager manager = (NotificationManager)getSystemService(NOTIFICATION_SERVICE);
        if (Build.VERSION.SDK_INT >= 26) {
            manager.createNotificationChannel(new NotificationChannel(
                CHANNEL_ID,
                "ProxyZapret VPN",
                NotificationManager.IMPORTANCE_LOW
            ));
        }

        PendingIntent open = PendingIntent.getActivity(
            this,
            0,
            new Intent(this, MainActivity.class),
            Build.VERSION.SDK_INT >= 23 ? PendingIntent.FLAG_IMMUTABLE : 0
        );

        Notification.Builder builder = Build.VERSION.SDK_INT >= 26
            ? new Notification.Builder(this, CHANNEL_ID)
            : new Notification.Builder(this);
        return builder
            .setContentTitle("ProxyZapret")
            .setContentText(text)
            .setSmallIcon(android.R.drawable.stat_sys_warning)
            .setContentIntent(open)
            .setOngoing(true)
            .build();
    }

    private void stopVpn(String status) {
        try {
            if (coreBridge != null) coreBridge.stop();
        } catch (Exception ignored) {
        }
        try {
            if (tun != null) tun.close();
        } catch (Exception ignored) {
        }
        coreBridge = null;
        tun = null;
        setStatus(false, status == null ? "Turn on to connect" : status);
        stopForeground(true);
        stopSelf();
    }

    private void setStatus(boolean running, String status) {
        getSharedPreferences(AppState.PREFS, MODE_PRIVATE)
            .edit()
            .putBoolean(AppState.KEY_RUNNING, running)
            .putString(AppState.KEY_STATUS, status == null ? "" : status)
            .apply();
    }
}
