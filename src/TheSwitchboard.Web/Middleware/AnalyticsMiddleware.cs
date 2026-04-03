using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Middleware;

public class AnalyticsMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> ExcludedPrefixes =
    [
        "/api/", "/health", "/admin/", "/_", "/css/", "/js/", "/images/", "/fonts/", "/favicon"
    ];

    public AnalyticsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAnalyticsService analytics)
    {
        await _next(context);

        // Only track successful page loads (not assets, not APIs, not errors)
        if (context.Response.StatusCode != 200)
            return;

        var path = context.Request.Path.Value ?? "";
        if (ExcludedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return;

        // Only track GET requests (page views)
        if (!HttpMethods.IsGet(context.Request.Method))
            return;

        var referrer = context.Request.Headers.Referer.FirstOrDefault();
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var sessionId = context.Request.Cookies["sw_sid"];

        // Fire and forget — don't slow down the response
        _ = Task.Run(async () =>
        {
            try
            {
                await analytics.RecordPageViewAsync(path, referrer, userAgent, ip, sessionId);
            }
            catch
            {
                // Silently fail — analytics should never break the site
            }
        });
    }
}
