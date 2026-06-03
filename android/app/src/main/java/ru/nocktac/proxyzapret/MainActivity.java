package ru.nocktac.proxyzapret;

import android.Manifest;
import android.app.Activity;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.net.VpnService;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

import ru.nocktac.proxyzapret.vpn.ProxyZapretVpnService;

public final class MainActivity extends Activity {
    private static final int REQUEST_VPN = 4001;
    private TextView status;
    private TextView detail;
    private Button toggle;
    private final Handler handler = new Handler(Looper.getMainLooper());

    @Override
    protected void onCreate(Bundle bundle) {
        super.onCreate(bundle);
        requestNotificationPermission();
        setContentView(createContent());
    }

    @Override
    protected void onResume() {
        super.onResume();
        updateUi();
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == REQUEST_VPN && resultCode == RESULT_OK) {
            startVpnService();
        }
    }

    private View createContent() {
        var root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setGravity(Gravity.CENTER_HORIZONTAL);
        root.setPadding(dp(28), dp(54), dp(28), dp(28));
        root.setBackgroundColor(0xFF0E121B);

        var title = new TextView(this);
        title.setText("ProxyZapret");
        title.setTextColor(0xFFFFFFFF);
        title.setTextSize(30);
        title.setGravity(Gravity.CENTER);
        title.setTypeface(android.graphics.Typeface.DEFAULT_BOLD);
        root.addView(title, new LinearLayout.LayoutParams(-1, -2));

        var version = new TextView(this);
        version.setText("Android preview 0.1.0");
        version.setTextColor(0xFF43D3A4);
        version.setTextSize(13);
        version.setGravity(Gravity.CENTER);
        root.addView(version, new LinearLayout.LayoutParams(-1, -2));

        var card = new LinearLayout(this);
        card.setOrientation(LinearLayout.VERTICAL);
        card.setGravity(Gravity.CENTER);
        card.setPadding(dp(22), dp(30), dp(22), dp(30));
        card.setBackgroundResource(R.drawable.card);
        var cardParams = new LinearLayout.LayoutParams(-1, dp(188));
        cardParams.setMargins(0, dp(34), 0, dp(30));
        root.addView(card, cardParams);

        status = new TextView(this);
        status.setTextColor(0xFFFFFFFF);
        status.setTextSize(22);
        status.setTypeface(android.graphics.Typeface.DEFAULT_BOLD);
        status.setGravity(Gravity.CENTER);
        card.addView(status, new LinearLayout.LayoutParams(-1, -2));

        detail = new TextView(this);
        detail.setTextColor(0xFF91A0B2);
        detail.setTextSize(14);
        detail.setGravity(Gravity.CENTER);
        var detailParams = new LinearLayout.LayoutParams(-1, -2);
        detailParams.setMargins(0, dp(12), 0, 0);
        card.addView(detail, detailParams);

        toggle = new Button(this);
        toggle.setAllCaps(false);
        toggle.setTextSize(16);
        toggle.setTypeface(android.graphics.Typeface.DEFAULT_BOLD);
        toggle.setOnClickListener(v -> toggle());
        var buttonParams = new LinearLayout.LayoutParams(-1, dp(58));
        buttonParams.setMargins(dp(22), 0, dp(22), 0);
        root.addView(toggle, buttonParams);

        var footer = new TextView(this);
        footer.setText("Only restricted services will be routed through ProxyZapret");
        footer.setTextColor(0xFF91A0B2);
        footer.setTextSize(12);
        footer.setGravity(Gravity.CENTER);
        var footerParams = new LinearLayout.LayoutParams(-1, -2);
        footerParams.setMargins(0, dp(32), 0, 0);
        root.addView(footer, footerParams);

        return root;
    }

    private void toggle() {
        if (isRunning()) {
            stopService(new Intent(this, ProxyZapretVpnService.class).setAction(AppState.ACTION_STOP));
            updateUiDelayed();
            return;
        }

        Intent prepare = VpnService.prepare(this);
        if (prepare != null) {
            startActivityForResult(prepare, REQUEST_VPN);
        } else {
            startVpnService();
        }
    }

    private void startVpnService() {
        toggle.setEnabled(false);
        status.setText("Starting...");
        detail.setText("Preparing managed subscription");
        var intent = new Intent(this, ProxyZapretVpnService.class).setAction(AppState.ACTION_START);
        if (Build.VERSION.SDK_INT >= 26) startForegroundService(intent);
        else startService(intent);
        updateUiDelayed();
    }

    private void updateUiDelayed() {
        handler.postDelayed(this::updateUi, 900);
    }

    private void updateUi() {
        boolean running = isRunning();
        String message = prefs().getString(AppState.KEY_STATUS, "");
        toggle.setEnabled(true);
        toggle.setText(running ? "Turn off" : "Turn on");
        toggle.setTextColor(running ? 0xFFE1E7F0 : 0xFF0E121B);
        toggle.setBackgroundResource(running
            ? R.drawable.button_secondary
            : R.drawable.button_primary);
        status.setText(running ? "Protection is active" : "Protection is off");
        detail.setText(message.isEmpty()
            ? (running ? "Restricted services use the secure route" : "Tap once to connect")
            : message);
    }

    private boolean isRunning() {
        return prefs().getBoolean(AppState.KEY_RUNNING, false);
    }

    private SharedPreferences prefs() {
        return getSharedPreferences(AppState.PREFS, MODE_PRIVATE);
    }

    private void requestNotificationPermission() {
        if (Build.VERSION.SDK_INT >= 33 &&
            checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) {
            requestPermissions(new String[] { Manifest.permission.POST_NOTIFICATIONS }, 4002);
        }
    }

    private int dp(int value) {
        return (int)(value * getResources().getDisplayMetrics().density + 0.5f);
    }
}
