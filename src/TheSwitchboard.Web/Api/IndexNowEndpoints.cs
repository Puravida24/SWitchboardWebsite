using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Api;

/// <summary>
/// Serves the IndexNow key verification file at <c>GET /{key}.txt</c>.
/// Returns 200 with the key as text/plain when the path matches the configured
/// key exactly, 404 otherwise. Defined as an endpoint (not a static wwwroot
/// file) so the key rotates purely via config — no filesystem change needed.
/// </summary>
public static class IndexNowEndpoints
{
    public static void MapIndexNowEndpoints(this WebApplication app)
    {
        app.MapGet("/{key}.txt", (string key, IIndexNowService svc) =>
        {
            var matched = svc.GetKeyIfMatches(key);
            if (matched is null) return Results.NotFound();
            return Results.Text(matched, "text/plain");
        });
    }
}
