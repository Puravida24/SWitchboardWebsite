using System.Text.Json;

namespace TheSwitchboard.Web.Api.Tracking;

/// <summary>
/// Tracker ingest endpoints.
///
/// T-1 ships the heartbeat: <c>POST /api/tracking/ping</c> returns 204 and
/// logs the visitor + session id + path for the admin Health page. Subsequent
/// slices attach <c>/pageview</c>, <c>/clicks</c>, <c>/scroll</c>, <c>/mouse-trail</c>,
/// <c>/form-events</c>, <c>/vitals</c>, <c>/errors</c>, <c>/signals</c>, <c>/consent</c>,
/// <c>/replay/chunk</c> onto the same <see cref="MapTrackingEndpoints"/>.
/// </summary>
public static class TrackingEndpoints
{
    // Duplicated from ContactEndpoints.IsOriginAllowed — private there, intentionally
    // small, copy-pasted rather than exposed publicly to keep the surface tight.
    private static bool IsOriginAllowed(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        if (string.IsNullOrEmpty(origin)) return true; // server-to-server, health, curl
        var host = context.Request.Host.Value;
        var scheme = context.Request.Scheme;
        return string.Equals(origin, $"{scheme}://{host}", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(origin, $"https://{host}",  StringComparison.OrdinalIgnoreCase) ||
               string.Equals(origin, $"http://{host}",   StringComparison.OrdinalIgnoreCase);
    }

    public sealed class PingRequest
    {
        public string? Vid { get; set; }
        public string? Sid { get; set; }
        public string? Path { get; set; }
        public DateTime? Ts { get; set; }
        public string? ConsentState { get; set; }
    }

    public static void MapTrackingEndpoints(this WebApplication app)
    {
        // T-1 heartbeat — returns 204 and logs. No DB write at T-1; that lands in T-3
        // when ISessionService starts upserting Session rows from every tracker event.
        app.MapPost("/api/tracking/ping", (
            PingRequest? request,
            HttpContext context,
            ILogger<TrackingPingMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var vid = request?.Vid ?? "(anon)";
            var sid = request?.Sid ?? "(anon)";
            var path = request?.Path ?? "/";
            logger.LogInformation(
                "Tracking ping vid={Vid} sid={Sid} path={Path} consent={Consent}",
                vid, sid, path, request?.ConsentState ?? "none");

            return Results.StatusCode(StatusCodes.Status204NoContent);
        });
    }

    /// <summary>
    /// Empty marker type so <see cref="ILogger{TCategoryName}"/> gets a meaningful
    /// category without polluting the Api namespace.
    /// </summary>
    public sealed class TrackingPingMarker { }
}
