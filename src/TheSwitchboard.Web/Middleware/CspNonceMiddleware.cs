using System.Security.Cryptography;

namespace TheSwitchboard.Web.Middleware;

/// <summary>
/// Generates a per-request 128-bit random nonce (base64, URL-safe-ish) and stashes it
/// on <c>HttpContext.Items["csp-nonce"]</c> for downstream consumers:
///  - <see cref="SecurityHeadersMiddleware"/> emits it in the CSP header.
///  - <see cref="TheSwitchboard.Web.Pages.PublicPageModel"/> substitutes it into
///    <c>{{NONCE}}</c> tokens in the served HTML.
///
/// MUST run earlier in the pipeline than SecurityHeadersMiddleware.
/// </summary>
public class CspNonceMiddleware
{
    public const string ContextKey = "csp-nonce";

    private readonly RequestDelegate _next;

    public CspNonceMiddleware(RequestDelegate next) { _next = next; }

    public async Task InvokeAsync(HttpContext context)
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        var nonce = Convert.ToBase64String(bytes);
        context.Items[ContextKey] = nonce;
        await _next(context);
    }

    public static string GetNonce(HttpContext ctx) =>
        ctx.Items[ContextKey] as string ?? string.Empty;
}
