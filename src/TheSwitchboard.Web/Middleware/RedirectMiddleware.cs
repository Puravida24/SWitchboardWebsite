using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Middleware;

/// <summary>
/// Checks the Redirects table for the current request path and issues a 301/302 if matched.
/// Runs early in the pipeline (before authentication + Razor Pages routing) so old URLs
/// never hit the page handler.
/// </summary>
public class RedirectMiddleware
{
    private readonly RequestDelegate _next;

    public RedirectMiddleware(RequestDelegate next) { _next = next; }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value;
        if (!string.IsNullOrEmpty(path))
        {
            var match = await db.Redirects.FirstOrDefaultAsync(r =>
                r.FromPath.ToLower() == path.ToLower());
            if (match is not null)
            {
                context.Response.StatusCode = match.StatusCode;
                context.Response.Headers.Location = match.ToPath;
                return;
            }
        }
        await _next(context);
    }
}
