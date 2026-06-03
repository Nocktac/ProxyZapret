package ru.nocktac.proxyzapret.subscription;

import android.content.Context;
import android.provider.Settings;
import android.util.Base64;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URI;
import java.net.URL;
import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;

public final class RemnawaveClient {
    private final Context context;
    private final String subscriptionUrl;

    public RemnawaveClient(Context context, String subscriptionUrl) {
        this.context = context.getApplicationContext();
        this.subscriptionUrl = subscriptionUrl;
    }

    public Subscription load() throws Exception {
        JSONObject json = new JSONObject(get("ProxyZapret/0.2 Android"));
        String standard = get("ProxyZapretAndroid/0.1 Android");
        List<ManagedNode> nodes = parseStandardSubscription(standard);
        nodes.sort(Comparator.comparingInt(node -> node.priority));
        return new Subscription(json, nodes);
    }

    private String get(String userAgent) throws Exception {
        HttpURLConnection connection = (HttpURLConnection)new URL(subscriptionUrl).openConnection();
        connection.setRequestProperty("User-Agent", userAgent);
        connection.setRequestProperty("x-hwid", hwid());
        connection.setRequestProperty("x-device-os", "Android");
        connection.setConnectTimeout(12000);
        connection.setReadTimeout(20000);
        try (BufferedReader reader = new BufferedReader(new InputStreamReader(connection.getInputStream(), StandardCharsets.UTF_8))) {
            StringBuilder result = new StringBuilder();
            String line;
            while ((line = reader.readLine()) != null) result.append(line).append('\n');
            return result.toString().trim();
        }
    }

    private String hwid() {
        String id = Settings.Secure.getString(context.getContentResolver(), Settings.Secure.ANDROID_ID);
        return id == null || id.trim().isEmpty() ? "android-unknown" : id;
    }

    private List<ManagedNode> parseStandardSubscription(String encoded) throws Exception {
        List<ManagedNode> result = new ArrayList<>();
        String decoded = new String(Base64.decode(encoded.replaceAll("\\s+", ""), Base64.DEFAULT), StandardCharsets.UTF_8);
        for (String raw : decoded.split("\\r?\\n")) {
            ManagedNode node = parseUri(raw.trim());
            if (node != null) result.add(node);
        }
        return result;
    }

    private ManagedNode parseUri(String value) {
        try {
            if (value.startsWith("ss://")) return parseShadowsocks(value);
            URI uri = URI.create(value);
            String scheme = uri.getScheme() == null ? "" : uri.getScheme().toLowerCase();
            if ("hysteria2".equals(scheme) || "hy2".equals(scheme)) return parseHysteria2(uri);
            if ("vless".equals(scheme)) return parseVless(uri);
            if ("trojan".equals(scheme)) return parseTrojan(uri);
        } catch (Exception ignored) {
        }
        return null;
    }

    private ManagedNode parseHysteria2(URI uri) throws Exception {
        JSONObject query = query(uri);
        JSONObject outbound = base("hysteria2", uri);
        outbound.put("password", decode(uri.getUserInfo()));
        outbound.put("tls", tls(query, true));
        if (query.has("obfs")) {
            JSONObject obfs = new JSONObject().put("type", query.getString("obfs"));
            if (query.has("obfs-password")) obfs.put("password", query.getString("obfs-password"));
            outbound.put("obfs", obfs);
        }
        return new ManagedNode(10, outbound);
    }

    private ManagedNode parseVless(URI uri) throws Exception {
        JSONObject query = query(uri);
        JSONObject outbound = base("vless", uri);
        outbound.put("uuid", decode(uri.getUserInfo()));
        if (query.has("flow")) outbound.put("flow", query.getString("flow"));
        if (query.has("packetEncoding")) outbound.put("packet_encoding", query.getString("packetEncoding"));
        applyTlsAndTransport(outbound, query);
        return new ManagedNode(20, outbound);
    }

    private ManagedNode parseTrojan(URI uri) throws Exception {
        JSONObject query = query(uri);
        JSONObject outbound = base("trojan", uri);
        outbound.put("password", decode(uri.getUserInfo()));
        applyTlsAndTransport(outbound, query);
        return new ManagedNode(30, outbound);
    }

    private ManagedNode parseShadowsocks(String value) throws Exception {
        String tag = "shadowsocks";
        int fragment = value.indexOf('#');
        if (fragment >= 0) {
            tag = decode(value.substring(fragment + 1));
            value = value.substring(0, fragment);
        }
        int query = value.indexOf('?');
        if (query >= 0) value = value.substring(0, query);

        String content = value.substring("ss://".length());
        String credentials;
        String hostPort;
        int at = content.lastIndexOf('@');
        if (at >= 0) {
            credentials = decodeBase64Url(content.substring(0, at));
            hostPort = content.substring(at + 1);
        } else {
            String decoded = decodeBase64Url(content);
            at = decoded.lastIndexOf('@');
            if (at < 0) return null;
            credentials = decoded.substring(0, at);
            hostPort = decoded.substring(at + 1);
        }
        int colon = credentials.indexOf(':');
        if (colon <= 0) return null;
        URI server = URI.create("ss://" + hostPort);
        JSONObject outbound = new JSONObject()
            .put("type", "shadowsocks")
            .put("tag", tag)
            .put("server", server.getHost())
            .put("server_port", server.getPort())
            .put("method", credentials.substring(0, colon))
            .put("password", credentials.substring(colon + 1));
        return new ManagedNode(40, outbound);
    }

    private JSONObject base(String type, URI uri) throws Exception {
        if (uri.getHost() == null || uri.getPort() <= 0 || uri.getUserInfo() == null) {
            throw new IllegalArgumentException("URI is incomplete");
        }
        return new JSONObject()
            .put("type", type)
            .put("tag", tag(uri, type))
            .put("server", uri.getHost())
            .put("server_port", uri.getPort());
    }

    private void applyTlsAndTransport(JSONObject outbound, JSONObject query) throws Exception {
        String security = query.optString("security", "");
        if ("tls".equalsIgnoreCase(security) || "reality".equalsIgnoreCase(security)) {
            outbound.put("tls", tls(query, true));
        }
        String type = query.optString("type", "tcp");
        if (!type.isEmpty() && !"tcp".equalsIgnoreCase(type)) {
            JSONObject transport = new JSONObject().put("type", type);
            if ("ws".equalsIgnoreCase(type)) {
                if (query.has("path")) transport.put("path", query.getString("path"));
                if (query.has("host")) transport.put("headers", new JSONObject().put("Host", query.getString("host")));
            } else if ("grpc".equalsIgnoreCase(type) && query.has("serviceName")) {
                transport.put("service_name", query.getString("serviceName"));
            }
            outbound.put("transport", transport);
        }
    }

    private JSONObject tls(JSONObject query, boolean enabled) throws Exception {
        JSONObject tls = new JSONObject().put("enabled", enabled);
        if (query.has("sni")) tls.put("server_name", query.getString("sni"));
        if (query.has("insecure")) tls.put("insecure", truthy(query.getString("insecure")));
        if (query.has("fp")) tls.put("utls", new JSONObject().put("enabled", true).put("fingerprint", query.getString("fp")));
        if ("reality".equalsIgnoreCase(query.optString("security"))) {
            JSONObject reality = new JSONObject().put("enabled", true);
            if (query.has("pbk")) reality.put("public_key", query.getString("pbk"));
            if (query.has("sid")) reality.put("short_id", query.getString("sid"));
            tls.put("reality", reality);
        }
        return tls;
    }

    private JSONObject query(URI uri) throws Exception {
        JSONObject result = new JSONObject();
        String raw = uri.getRawQuery();
        if (raw == null || raw.isEmpty()) return result;
        for (String item : raw.split("&")) {
            String[] parts = item.split("=", 2);
            result.put(decode(parts[0]), parts.length == 2 ? decode(parts[1]) : "");
        }
        return result;
    }

    private String tag(URI uri, String fallback) {
        String fragment = uri.getRawFragment();
        return fragment == null || fragment.isEmpty() ? fallback : decode(fragment);
    }

    private boolean truthy(String value) {
        return "1".equals(value) || "true".equalsIgnoreCase(value);
    }

    private String decode(String value) {
        try {
            return value == null ? "" : URLDecoder.decode(value, "UTF-8");
        } catch (Exception ignored) {
            return value == null ? "" : value;
        }
    }

    private String decodeBase64Url(String value) {
        try {
            String normalized = decode(value).replace('-', '+').replace('_', '/');
            while (normalized.length() % 4 != 0) normalized += "=";
            return new String(Base64.decode(normalized, Base64.DEFAULT), StandardCharsets.UTF_8);
        } catch (Exception ignored) {
            return decode(value);
        }
    }
}
