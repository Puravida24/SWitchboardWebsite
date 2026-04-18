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
            // style-src keeps 'unsafe-inline' for now — the wireframe ships with
            // one giant inline <style> block; splitting out to external CSS is a
            // follow-up. Low XSS leverage compared to script injection.
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';";

        await _next(context);
    }
}
