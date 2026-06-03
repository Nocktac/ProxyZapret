package ru.nocktac.proxyzapret.subscription;

import android.content.Context;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.File;
import java.io.FileOutputStream;
import java.nio.charset.StandardCharsets;

public final class ConfigBuilder {
    private final Context context;

    public ConfigBuilder(Context context) {
        this.context = context.getApplicationContext();
    }

    public JSONObject build(Subscription subscription) throws Exception {
        JSONArray nodes = managedNodes(subscription);
        JSONArray tags = new JSONArray();
        JSONArray outbounds = new JSONArray().put(new JSONObject().put("type", "direct").put("tag", "direct"));
        for (int index = 0; index < nodes.length(); index++) {
            JSONObject node = nodes.getJSONObject(index);
            String tag = index == 0 ? "managed-primary" : "managed-backup";
            node.put("tag", tag);
            node.remove("network");
            tags.put(tag);
            outbounds.put(node);
        }
        outbounds.put(new JSONObject()
            .put("type", "urltest")
            .put("tag", "managed-auto")
            .put("outbounds", tags)
            .put("url", "https://www.gstatic.com/generate_204")
            .put("interval", "1m")
            .put("tolerance", 100)
            .put("interrupt_exist_connections", true));

        return new JSONObject()
            .put("log", new JSONObject().put("level", "info").put("timestamp", true))
            .put("dns", dns())
            .put("inbounds", new JSONArray().put(new JSONObject()
                .put("type", "tun")
                .put("tag", "tun-in")
                .put("interface_name", "ProxyZapret")
                .put("address", new JSONArray().put("172.19.0.1/30"))
                .put("auto_route", false)
                .put("strict_route", true)
                .put("stack", "mixed")))
            .put("outbounds", outbounds)
            .put("route", route());
    }

    public void save(JSONObject config, File target) throws Exception {
        try (FileOutputStream stream = new FileOutputStream(target)) {
            stream.write(config.toString(2).getBytes(StandardCharsets.UTF_8));
        }
    }

    private JSONArray managedNodes(Subscription subscription) throws Exception {
        JSONArray result = new JSONArray();
        if (subscription.uriNodes.size() >= 2) {
            result.put(new JSONObject(subscription.uriNodes.get(0).outbound.toString()));
            result.put(new JSONObject(subscription.uriNodes.get(1).outbound.toString()));
            return result;
        }

        JSONArray outbounds = subscription.jsonSubscription.getJSONArray("outbounds");
        for (int index = 0; index < outbounds.length() && result.length() < 2; index++) {
            JSONObject outbound = outbounds.getJSONObject(index);
            String type = outbound.optString("type");
            if ("direct".equals(type) || "block".equals(type) || "dns".equals(type) ||
                "selector".equals(type) || "urltest".equals(type)) {
                continue;
            }
            result.put(new JSONObject(outbound.toString()));
        }
        if (result.length() == 0) throw new IllegalStateException("Subscription does not contain supported nodes.");
        return result;
    }

    private JSONObject dns() throws Exception {
        return new JSONObject()
            .put("servers", new JSONArray().put(new JSONObject()
                .put("type", "https")
                .put("tag", "public-dns")
                .put("server", "1.1.1.1")
                .put("server_port", 443)
                .put("path", "/dns-query")
                .put("tls", new JSONObject().put("enabled", true).put("server_name", "cloudflare-dns.com"))))
            .put("final", "public-dns")
            .put("strategy", "ipv4_only")
            .put("reverse_mapping", true);
    }

    private JSONObject route() throws Exception {
        JSONArray ruleSets = new JSONArray()
            .put(ruleSet("ru-blocked-domains-all", "sing-box/rule-set-geosite/geosite-ru-blocked-all.srs"))
            .put(ruleSet("ru-blocked-ip", "sing-box/rule-set-geoip/geoip-ru-blocked.srs"))
            .put(ruleSet("service-discord", "sing-box/rule-set-geosite/geosite-discord.srs"))
            .put(ruleSet("service-telegram", "sing-box/rule-set-geosite/geosite-telegram.srs"))
            .put(ruleSet("service-telegram-ip", "sing-box/rule-set-geoip/geoip-telegram.srs"))
            .put(ruleSet("service-meta", "sing-box/rule-set-geosite/geosite-meta.srs"))
            .put(ruleSet("service-instagram", "sing-box/rule-set-geosite/geosite-instagram.srs"))
            .put(ruleSet("service-youtube", "sing-box/rule-set-geosite/geosite-youtube.srs"))
            .put(ruleSet("service-roblox", "sing-box/rule-set-geosite/geosite-roblox.srs"))
            .put(ruleSet("service-twitter", "sing-box/rule-set-geosite/geosite-twitter.srs"))
            .put(ruleSet("service-twitter-ip", "sing-box/rule-set-geoip/geoip-twitter.srs"))
            .put(ruleSet("service-tiktok", "sing-box/rule-set-geosite/geosite-tiktok.srs"))
            .put(ruleSet("service-whatsapp", "sing-box/rule-set-geosite/geosite-whatsapp.srs"));

        JSONArray rules = new JSONArray()
            .put(new JSONObject().put("action", "sniff"))
            .put(new JSONObject().put("protocol", "dns").put("action", "hijack-dns"))
            .put(new JSONObject().put("ip_is_private", true).put("action", "route").put("outbound", "direct"))
            .put(new JSONObject()
                .put("rule_set", new JSONArray()
                    .put("ru-blocked-domains-all").put("ru-blocked-ip")
                    .put("service-discord").put("service-telegram").put("service-telegram-ip")
                    .put("service-meta").put("service-instagram").put("service-youtube")
                    .put("service-roblox").put("service-twitter").put("service-twitter-ip")
                    .put("service-tiktok").put("service-whatsapp"))
                .put("action", "route")
                .put("outbound", "managed-auto"));

        return new JSONObject()
            .put("auto_detect_interface", true)
            .put("default_domain_resolver", "public-dns")
            .put("rule_set", ruleSets)
            .put("rules", rules)
            .put("final", "direct");
    }

    private JSONObject ruleSet(String tag, String path) throws Exception {
        return new JSONObject()
            .put("type", "remote")
            .put("tag", tag)
            .put("format", "binary")
            .put("url", "https://fastly.jsdelivr.net/gh/runetfreedom/russia-v2ray-rules-dat@release/" + path)
            .put("update_interval", "6h");
    }
}
