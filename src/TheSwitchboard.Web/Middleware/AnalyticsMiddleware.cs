using System.Security.Cryptography;
using System.Text;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Middleware;

public class AnalyticsMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> ExcludedPrefixes =
    [
        "/api/", "/health", "/admin/", "/_", "/css/", "/js/", "/images/", "/fonts/", "/favicon",
        "/sitemap.xml", "/robots.txt", "/llms.txt"
    ];

    public AnalyticsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAnalyticsService analytics, IConfiguration config)
    {
        await _next(context);

        // Only track successful page loads (not assets, not APIs, not errors)
        if (context.Response.StatusCode != 200) return;

        var path = context.Request.Path.Value ?? "";
        if (ExcludedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))) return;
        if (!HttpMethods.IsGet(context.Request.Method)) return;

        // S4-08 / GDPR: honor Do-Not-Track. If the visitor has set DNT=1, don't record.
        // Also honor "Sec-GPC: 1" (Global Privacy Control).
        if (context.Request.Headers.TryGetValue("DNT", out var dnt) && dnt.ToString() == "1") return;
        if (context.Request.Headers.TryGetValue("Sec-GPC", out var gpc) && gpc.ToString() == "1") return;

        var referrer = context.Request.Headers.Referer.FirstOrDefault();
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        var rawIp = context.Connection.RemoteIpAddress?.ToString();
        var sessionId = context.Request.Cookies["sw_sid"];
        var salt = config["Analytics:IpHashSalt"] ?? "default-salt-change-in-prod";
        var ipHash = string.IsNullOrEmpty(rawIp) ? null : HashIp(rawIp, salt);

        // Record synchronously but catch-and-ignore: the response is already written
        // (we called _next first), so the latency cost is just an async DB insert after
        // the response has been streamed. Using fire-and-forget Task.Run would lose
        // writes because IAnalyticsService's DbContext is request-scoped and disposes
        // the moment this middleware returns.
        try
        {
            await analytics.RecordPageViewAsync(path, referrer, userAgent, ipHash, sessionId);
        }
        catch { /* analytics must never break the site */ }
    }

    private static string HashIp(string ip, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes(ip + "|" + salt);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
