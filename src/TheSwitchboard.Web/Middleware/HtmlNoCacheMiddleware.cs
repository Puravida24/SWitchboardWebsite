namespace TheSwitchboard.Web.Middleware;

/// <summary>
/// H-9f: Because every HTML response carries a per-request CSP nonce,
/// any intermediate cache would serve stale nonces and break inline scripts.
/// Set explicit no-cache headers so browsers + CDN edges never cache the body.
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
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }
            return Task.CompletedTask;
        });
        await _next(context);
    }
}
