package ru.nocktac.proxyzapret;

import android.Manifest;
import android.app.Activity;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.LinearGradient;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.Shader;
import android.net.VpnService;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import ru.nocktac.proxyzapret.vpn.SingBoxCoreBridge;
import ru.nocktac.proxyzapret.vpn.ProxyZapretVpnService;

public final class MainActivity extends Activity {
    private static final int REQUEST_VPN = 4001;
    private TextView status;
    private TextView detail;
    private TextView badge;
    private Button toggle;
    private final Handler handler = new Handler(Looper.getMainLooper());
    private final Runnable refresh = new Runnable() {
        @Override
        public void run() {
            updateUi();
            handler.postDelayed(this, 1000);
        }
    };

    @Override
    protected void onCreate(Bundle bundle) {
        super.onCreate(bundle);
        requestNotificationPermission();
        setContentView(createContent());
    }

    @Override
    protected void onResume() {
        super.onResume();
        handler.removeCallbacks(refresh);
        refresh.run();
    }

    @Override
    protected void onPause() {
        handler.removeCallbacks(refresh);
        super.onPause();
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == REQUEST_VPN && resultCode == RESULT_OK) {
            startVpnService();
        }
    }

    private View createContent() {
        var scroll = new ScrollView(this);
        scroll.setFillViewport(true);
        scroll.setBackgroundColor(0xFF0E121B);

        var root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setGravity(Gravity.CENTER_HORIZONTAL);
        root.setPadding(dp(26), dp(44), dp(26), dp(26));
        root.setBackgroundColor(0xFF0E121B);
        scroll.addView(root, new ScrollView.LayoutParams(-1, -1));

        var logo = new LogoView(this);
        var logoParams = new LinearLayout.LayoutParams(dp(86), dp(86));
        logoParams.setMargins(0, 0, 0, dp(18));
        root.addView(logo, logoParams);

        var title = new TextView(this);
        title.setText("ProxyZapret");
        title.setTextColor(0xFFFFFFFF);
        title.setTextSize(30);
        title.setGravity(Gravity.CENTER);
        title.setTypeface(android.graphics.Typeface.DEFAULT_BOLD);
        root.addView(title, new LinearLayout.LayoutParams(-1, -2));

        var version = new TextView(this);
        version.setText("Android " + BuildConfig.VERSION_NAME);
        version.setTextColor(0xFF43D3A4);
        version.setTextSize(13);
        version.setGravity(Gravity.CENTER);
        root.addView(version, new LinearLayout.LayoutParams(-1, -2));

        badge = new TextView(this);
        badge.setTextSize(12);
        badge.setGravity(Gravity.CENTER);
        badge.setPadding(dp(12), dp(7), dp(12), dp(7));
        var badgeParams = new LinearLayout.LayoutParams(-2, -2);
        badgeParams.setMargins(0, dp(18), 0, 0);
        root.addView(badge, badgeParams);

        var card = new LinearLayout(this);
        card.setOrientation(LinearLayout.VERTICAL);
        card.setGravity(Gravity.CENTER);
        card.setPadding(dp(22), dp(30), dp(22), dp(30));
        card.setBackgroundResource(R.drawable.card);
        var cardParams = new LinearLayout.LayoutParams(-1, dp(196));
        cardParams.setMargins(0, dp(24), 0, dp(28));
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

        return scroll;
    }

    private void toggle() {
        if (!SingBoxCoreBridge.isAvailable()) {
            prefs().edit()
                .putBoolean(AppState.KEY_RUNNING, false)
                .putString(AppState.KEY_STATUS, SingBoxCoreBridge.unavailableReason())
                .putString(AppState.KEY_LAST_ERROR, SingBoxCoreBridge.unavailableReason())
                .apply();
            updateUi();
            return;
        }

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
        String error = prefs().getString(AppState.KEY_LAST_ERROR, "");
        boolean coreReady = SingBoxCoreBridge.isAvailable();
        toggle.setEnabled(true);
        toggle.setText(running ? "Turn off" : "Turn on");
        toggle.setTextColor(running ? 0xFFE1E7F0 : 0xFF0E121B);
        toggle.setBackgroundResource(running
            ? R.drawable.button_secondary
            : R.drawable.button_primary);
        status.setText(running ? "Protection is active" : (error.isEmpty() ? "Protection is off" : "Core is pending"));
        detail.setText(!error.isEmpty()
            ? error
            : message.isEmpty()
            ? (running ? "Restricted services use the secure route" : "Tap once to connect")
            : message);
        badge.setText(coreReady ? "Managed VPN ready" : "Preview build: VPN core pending");
        badge.setTextColor(coreReady ? 0xFF0E121B : 0xFFE1E7F0);
        badge.setBackgroundResource(coreReady ? R.drawable.button_primary : R.drawable.button_secondary);
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

    private static final class LogoView extends View {
        private final Paint paint = new Paint(Paint.ANTI_ALIAS_FLAG);

        LogoView(Activity activity) {
            super(activity);
        }

        @Override
        protected void onDraw(Canvas canvas) {
            super.onDraw(canvas);
            float size = Math.min(getWidth(), getHeight());
            float left = (getWidth() - size) / 2f;
            float top = (getHeight() - size) / 2f;
            paint.setStyle(Paint.Style.FILL);
            paint.setShader(new LinearGradient(left, top, left + size, top + size,
                Color.rgb(67, 211, 164), Color.rgb(55, 126, 255), Shader.TileMode.CLAMP));
            canvas.drawRoundRect(left + size * 0.08f, top + size * 0.08f,
                left + size * 0.92f, top + size * 0.92f, size * 0.24f, size * 0.24f, paint);
            paint.setShader(null);
            paint.setStyle(Paint.Style.STROKE);
            paint.setStrokeWidth(size * 0.06f);
            paint.setStrokeCap(Paint.Cap.ROUND);
            paint.setStrokeJoin(Paint.Join.ROUND);
            paint.setColor(Color.argb(230, 238, 246, 255));

            Path shield = new Path();
            shield.moveTo(left + size * 0.50f, top + size * 0.22f);
            shield.lineTo(left + size * 0.70f, top + size * 0.31f);
            shield.lineTo(left + size * 0.66f, top + size * 0.62f);
            shield.lineTo(left + size * 0.50f, top + size * 0.80f);
            shield.lineTo(left + size * 0.34f, top + size * 0.62f);
            shield.lineTo(left + size * 0.30f, top + size * 0.31f);
            shield.close();
            canvas.drawPath(shield, paint);

            Path check = new Path();
            check.moveTo(left + size * 0.39f, top + size * 0.51f);
            check.lineTo(left + size * 0.48f, top + size * 0.60f);
            check.lineTo(left + size * 0.64f, top + size * 0.43f);
            canvas.drawPath(check, paint);
        }
    }
}
