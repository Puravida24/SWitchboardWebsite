namespace TheSwitchboard.Web.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        var nonce = CspNonceMiddleware.GetNonce(context);

        headers["X-Content-Type-Options"] = "nosniff";
        // SAMEORIGIN (not DENY) so admin heatmap pages can iframe the public site
        // for click/scroll overlay previews. External embedding is still blocked.
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // Belt-and-suspenders with robots.txt: mark admin + API routes noindex even if
        // someone external links to them. robots.txt is advisory; this header is enforced.
        var path = context.Request.Path.Value;
        if (path is not null &&
            (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)))
        {
            headers["X-Robots-Tag"] = "noindex, nofollow";
        }
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            // H-05: script-src uses a per-request nonce — no 'unsafe-inline'.
            // H-04: 'unsafe-eval' dropped (our code doesn't eval).
            $"script-src 'self' 'nonce-{nonce}' https://cdn.tailwindcss.com https://cdn.jsdelivr.net; " +
            // H-07.1: moved the giant <style> block to /css/design-32e.css, but
            // the design relies on ~126 inline style="..." attributes for grid
            // layouts (Phoenix live-ops terminal, KPI strip, data rows). Removing
            // 'unsafe-inline' here strips those and the page renders unstyled, so
            // we keep it for style-src only. script-src remains nonce-based — the
            // bigger XSS surface.
            // H-6.B: fonts are now self-hosted under /fonts/ — dropped fonts.googleapis.com
            // and fonts.gstatic.com from the policy. Tighter CSP surface.
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            // T-7: worker-src for CompressionStream + rrweb in some browsers.
            "worker-src 'self' blob:; " +
            // T-7: frame-ancestors 'self' (relaxed from 'none') so admin heatmap
            // + Sessions/Detail pages can iframe the public site for previews.
            // External embedding still blocked by X-Frame-Options: SAMEORIGIN.
            "frame-ancestors 'self';";

        await _next(context);
    }
}
