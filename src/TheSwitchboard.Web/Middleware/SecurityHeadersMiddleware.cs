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
        headers["X-Frame-Options"] = "DENY";
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
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';";

        await _next(context);
    }
}
