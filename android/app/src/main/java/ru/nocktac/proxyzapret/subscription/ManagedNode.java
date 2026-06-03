package ru.nocktac.proxyzapret.subscription;

import org.json.JSONObject;

public final class ManagedNode {
    public final int priority;
    public final JSONObject outbound;

    public ManagedNode(int priority, JSONObject outbound) {
        this.priority = priority;
        this.outbound = outbound;
    }
}
