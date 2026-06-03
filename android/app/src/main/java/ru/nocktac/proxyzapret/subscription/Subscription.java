package ru.nocktac.proxyzapret.subscription;

import org.json.JSONObject;

import java.util.ArrayList;
import java.util.List;

public final class Subscription {
    public final JSONObject jsonSubscription;
    public final List<ManagedNode> uriNodes;

    public Subscription(JSONObject jsonSubscription, List<ManagedNode> uriNodes) {
        this.jsonSubscription = jsonSubscription;
        this.uriNodes = uriNodes == null ? new ArrayList<>() : uriNodes;
    }
}
