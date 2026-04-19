using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Analytics;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Api.Tracking;

/// <summary>
/// Tracker ingest endpoints. Every endpoint is same-origin-gated (403 on foreign Origin)
/// and returns <c>204 NoContent</c> — the client never waits on analytics.
///
/// T-1:  <c>POST /api/tracking/ping</c>     — heartbeat
/// T-2:  <c>POST /api/tracking/pageview</c> — enriched pageview with UTM/click-ids/UA/viewport
///
/// Subsequent slices add <c>/signals</c>, <c>/clicks</c>, <c>/scroll</c>, <c>/mouse-trail</c>,
/// <c>/form-events</c>, <c>/vitals</c>, <c>/errors</c>, <c>/consent</c>, <c>/replay/chunk</c>.
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

    private static string HashIp(string ip, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes(ip + "|" + salt);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    public sealed class PingRequest
    {
        public string? Vid { get; set; }
        public string? Sid { get; set; }
        public string? Path { get; set; }
        public DateTime? Ts { get; set; }
        public string? ConsentState { get; set; }
    }

    public sealed class PageviewRequest
    {
        public string? Vid { get; set; }
        public string? Sid { get; set; }
        public string? Path { get; set; }
        public string? Referrer { get; set; }
        public string? UserAgent { get; set; }
        public string? UtmSource { get; set; }
        public string? UtmMedium { get; set; }
        public string? UtmCampaign { get; set; }
        public string? UtmTerm { get; set; }
        public string? UtmContent { get; set; }
        public string? Gclid { get; set; }
        public string? Fbclid { get; set; }
        public string? Msclkid { get; set; }
        public int? ViewportW { get; set; }
        public int? ViewportH { get; set; }
        public DateTime? Ts { get; set; }
        public string? ConsentState { get; set; }
    }

    public static void MapTrackingEndpoints(this WebApplication app)
    {
        // T-1 heartbeat — returns 204 and logs.
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

        // T-2 enriched pageview. Writes one PageView row, derives LandingFlag from
        // "is there a prior PV on this sid?", and parses the UA server-side so the
        // client never sends parsed device/browser/os (preventing spoofed metrics).
        app.MapPost("/api/tracking/pageview", async (
            PageviewRequest? request,
            HttpContext context,
            AppDbContext db,
            IConfiguration config,
            ILogger<TrackingPageviewMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (request is null)
                return Results.StatusCode(StatusCodes.Status204NoContent);

            // DNT/GPC parity with server middleware. Belt-and-suspenders against a
            // misbehaving tracker.js that somehow fired past the client-side gate.
            if (context.Request.Headers.TryGetValue("DNT", out var dnt) && dnt.ToString() == "1")
                return Results.StatusCode(StatusCodes.Status204NoContent);
            if (context.Request.Headers.TryGetValue("Sec-GPC", out var gpc) && gpc.ToString() == "1")
                return Results.StatusCode(StatusCodes.Status204NoContent);

            var path = string.IsNullOrWhiteSpace(request.Path) ? "/" : request.Path!;
            var sid = request.Sid;
            var vid = request.Vid;

            // LandingFlag = first PV for this sid. Cheap index hit on (SessionId, Timestamp).
            var landing = true;
            if (!string.IsNullOrEmpty(sid))
            {
                landing = !await db.PageViews.AnyAsync(p => p.SessionId == sid);
            }

            // Prefer the client-reported UA (it's the same string the browser sent in
            // headers, but we accept it from the body so server-less sources like
            // /api/tracking/pageview manual curl tests can exercise the parser).
            var ua = !string.IsNullOrWhiteSpace(request.UserAgent)
                ? request.UserAgent
                : context.Request.Headers.UserAgent.FirstOrDefault();
            var parsed = UserAgentParser.Parse(ua);

            var rawIp = context.Connection.RemoteIpAddress?.ToString();
            var salt = config["Analytics:IpHashSalt"] ?? "default-salt-change-in-prod";
            var ipHash = string.IsNullOrEmpty(rawIp) ? null : HashIp(rawIp, salt);

            var pv = new PageView
            {
                Path = path,
                Referrer = request.Referrer ?? context.Request.Headers.Referer.FirstOrDefault(),
                UserAgent = ua,
                IpHash = ipHash,
                SessionId = sid,
                VisitorId = vid,
                UtmSource = request.UtmSource,
                UtmMedium = request.UtmMedium,
                UtmCampaign = request.UtmCampaign,
                UtmTerm = request.UtmTerm,
                UtmContent = request.UtmContent,
                Gclid = request.Gclid,
                Fbclid = request.Fbclid,
                Msclkid = request.Msclkid,
                LandingFlag = landing,
                DeviceType = parsed.DeviceType,
                Browser = parsed.Browser,
                Os = parsed.Os,
                ViewportW = request.ViewportW,
                ViewportH = request.ViewportH,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                db.PageViews.Add(pv);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Analytics MUST NEVER break the site — log and swallow.
                logger.LogWarning(ex, "Pageview persist failed for sid={Sid} path={Path}", sid, path);
            }

            return Results.StatusCode(StatusCodes.Status204NoContent);
        });
    }

    /// <summary>ILogger category marker for ping.</summary>
    public sealed class TrackingPingMarker { }
    /// <summary>ILogger category marker for pageview.</summary>
    public sealed class TrackingPageviewMarker { }
}
