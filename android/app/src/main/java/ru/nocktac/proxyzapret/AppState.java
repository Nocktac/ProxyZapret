package ru.nocktac.proxyzapret;

public final class AppState {
    public static final String PREFS = "proxyzapret";
    public static final String KEY_RUNNING = "running";
    public static final String KEY_STATUS = "status";
    public static final String KEY_LAST_ERROR = "last_error";

    public static final String ACTION_START = "ru.nocktac.proxyzapret.START";
    public static final String ACTION_STOP = "ru.nocktac.proxyzapret.STOP";

    private AppState() {
    }
}
