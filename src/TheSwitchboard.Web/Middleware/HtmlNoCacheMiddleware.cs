namespace TheSwitchboard.Web.Middleware;

/// <summary>
/// H-9f: Every HTML response carries a per-request CSP nonce, so intermediate
/// HTTP caches (browser disk cache, CDN edges) must NOT store the body — a
/// stale nonce would break inline scripts on revisit.
///
/// Use `no-cache, must-revalidate` (not `no-store`). `no-cache` requires the
/// browser to revalidate with the server on every navigation (origin controls
/// freshness); `no-store` would have also blocked the browser back/forward
/// cache, which is a memory snapshot — bfcache preserves headers + body
/// together so the nonce remains consistent on restore (Lighthouse bf-cache
/// audit flags no-store specifically).
///
/// Static assets (/css, /js, /fonts, /wireframes/assets) still get the long-lived
/// `public, max-age=604800, immutable` cache header set by StaticFileOptions.
/// </summary>
public class HtmlNoCacheMiddleware
{
    private readonly RequestDelegate _next;

    public HtmlNoCacheMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var ct = context.Response.ContentType ?? string.Empty;
            if (ct.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers["Cache-Control"] = "no-cache, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }
            return Task.CompletedTask;
        });
        await _next(context);
    }
}
