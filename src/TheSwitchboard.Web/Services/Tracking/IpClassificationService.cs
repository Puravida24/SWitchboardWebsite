namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// UA-heuristic bot classifier. T-3 scope: inspect the user agent for headless
/// automation, known in-app WebViews with quirky behavior, and obvious crawler
/// tokens. Returns a (IsBot, Reason) pair so Session rows get a human-readable
/// BotReason like "headless" / "crawler" / "meta-webview".
///
/// Full IP→ASN lookup against <c>KnownProxyAsn</c> is deferred to a later slice —
/// requires a MaxMind or equivalent ASN database we haven't yet vendored. Sessions
/// get classified on the quick wins; paid traffic already filters out most of the
/// ASN-proxy surface before it reaches the site.
/// </summary>
public interface IIpClassificationService
{
    (bool IsBot, string? Reason) Classify(string? userAgent, string? ip);
}

public class IpClassificationService : IIpClassificationService
{
    public (bool IsBot, string? Reason) Classify(string? userAgent, string? ip)
    {
        var ua = userAgent ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ua)) return (false, null);

        // Automation / headless — hardest tell in UA.
        if (Contains(ua, "HeadlessChrome")) return (true, "headless");
        if (Contains(ua, "PhantomJS"))      return (true, "headless");
        if (Contains(ua, "Puppeteer"))      return (true, "headless");
        if (Contains(ua, "Playwright"))     return (true, "headless");
        if (Contains(ua, "Chrome-Lighthouse")) return (true, "lighthouse");

        // Known crawlers — bucket under "crawler" so admins can toggle include/exclude.
        string[] crawlerTokens =
        {
            "Googlebot", "Bingbot", "Slurp", "DuckDuckBot", "BingPreview",
            "YandexBot", "Baiduspider", "facebookexternalhit", "Facebot",
            "LinkedInBot", "Twitterbot", "AhrefsBot", "SemrushBot", "MJ12bot",
            "DotBot", "AwarioBot", "archive.org_bot", "ia_archiver",
            "Applebot", "Bytespider", "GPTBot", "CCBot", "ClaudeBot", "Anthropic"
        };
        foreach (var t in crawlerTokens)
            if (Contains(ua, t)) return (true, "crawler");

        // Generic "bot" / "spider" token fallback.
        if (Contains(ua, "bot") || Contains(ua, "spider")) return (true, "crawler");

        return (false, null);
    }

    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
