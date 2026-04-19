namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Zero-dependency UA parser. Emits three strings per pageview:
///   DeviceType ∈ { "mobile", "tablet", "desktop", "bot", "unknown" }
///   Browser    ∈ { "Chrome", "Safari", "Firefox", "Edge", "Opera", "Samsung", "IE", "Other" }
///   Os         ∈ { "iOS", "Android", "Windows", "macOS", "Linux", "ChromeOS", "Other" }
///
/// The parser is intentionally simple — coverage &gt; precision. We bucket traffic for
/// reports, not for fingerprinting. Order matters (iPadOS reports as Mac, headless Chrome
/// reports as Chrome, Edge reports Chromium+Edg, etc.).
/// </summary>
public static class UserAgentParser
{
    public sealed record Result(string DeviceType, string Browser, string Os);

    public static Result Parse(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return new("unknown", "Other", "Other");
        var u = ua;

        var os = ParseOs(u);
        var device = ParseDevice(u, os);
        var browser = ParseBrowser(u);
        return new(device, browser, os);
    }

    private static string ParseOs(string u)
    {
        if (u.Contains("iPhone", StringComparison.Ordinal) ||
            u.Contains("iPad", StringComparison.Ordinal) ||
            u.Contains("iPod", StringComparison.Ordinal) ||
            u.Contains("iPhone OS", StringComparison.Ordinal)) return "iOS";
        if (u.Contains("Android", StringComparison.Ordinal)) return "Android";
        if (u.Contains("Windows", StringComparison.Ordinal)) return "Windows";
        if (u.Contains("CrOS", StringComparison.Ordinal)) return "ChromeOS";
        // Mac must come after iOS/iPad check because iPadOS 13+ UA says "Macintosh"
        // but we'd only get here if iPhone/iPad/iPod weren't found.
        if (u.Contains("Mac OS X", StringComparison.Ordinal) ||
            u.Contains("Macintosh", StringComparison.Ordinal)) return "macOS";
        if (u.Contains("Linux", StringComparison.Ordinal)) return "Linux";
        return "Other";
    }

    private static string ParseDevice(string u, string os)
    {
        // Known bot tokens first — bucket them so they don't skew device counts.
        if (LooksLikeBot(u)) return "bot";

        if (os == "iOS")
        {
            if (u.Contains("iPad", StringComparison.Ordinal)) return "tablet";
            return "mobile";
        }
        if (os == "Android")
        {
            // Android tablets *usually* omit "Mobile" in the UA per Google's spec.
            return u.Contains("Mobile", StringComparison.Ordinal) ? "mobile" : "tablet";
        }
        if (u.Contains("Mobile", StringComparison.Ordinal)) return "mobile";
        return "desktop";
    }

    private static bool LooksLikeBot(string u)
    {
        // Cheap substring sweep covering the heavy hitters. Full ASN/IP bot work lands in T-3.
        string[] tokens =
        [
            "bot", "spider", "crawler", "slurp", "duckduck", "bingpreview",
            "headlesschrome", "phantomjs", "puppeteer", "chrome-lighthouse",
            "yandex", "baiduspider", "ahrefsbot", "semrush", "mj12bot"
        ];
        foreach (var t in tokens)
            if (u.Contains(t, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static string ParseBrowser(string u)
    {
        // Order: Edge before Chrome (Edg token appears in Chromium UA), Opera
        // before Chrome (OPR token), Chrome before Safari (Chrome UA also
        // has Safari), Samsung before Chrome.
        if (u.Contains("Edg/", StringComparison.Ordinal) ||
            u.Contains("Edge/", StringComparison.Ordinal) ||
            u.Contains("EdgiOS/", StringComparison.Ordinal) ||
            u.Contains("EdgA/", StringComparison.Ordinal)) return "Edge";
        if (u.Contains("OPR/", StringComparison.Ordinal) ||
            u.Contains("Opera", StringComparison.Ordinal)) return "Opera";
        if (u.Contains("SamsungBrowser", StringComparison.Ordinal)) return "Samsung";
        if (u.Contains("Firefox", StringComparison.Ordinal) ||
            u.Contains("FxiOS", StringComparison.Ordinal)) return "Firefox";
        if (u.Contains("CriOS", StringComparison.Ordinal) ||
            u.Contains("Chrome/", StringComparison.Ordinal)) return "Chrome";
        if (u.Contains("Safari/", StringComparison.Ordinal)) return "Safari";
        if (u.Contains("MSIE", StringComparison.Ordinal) ||
            u.Contains("Trident/", StringComparison.Ordinal)) return "IE";
        return "Other";
    }
}
