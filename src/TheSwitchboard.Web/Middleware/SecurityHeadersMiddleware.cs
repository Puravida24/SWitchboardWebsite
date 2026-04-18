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
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            // H-05: script-src uses a per-request nonce — no 'unsafe-inline'.
            // H-04: 'unsafe-eval' dropped (our code doesn't eval).
            $"script-src 'self' 'nonce-{nonce}' https://cdn.tailwindcss.com https://cdn.jsdelivr.net; " +
            // H-07.1: style-src tightened — the giant inline <style> block moved
            // to /css/design-32e.css. 'unsafe-inline' removed. Font services still
            // allowed.
            "style-src 'self' https://fonts.googleapis.com; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';";

        await _next(context);
    }
}
