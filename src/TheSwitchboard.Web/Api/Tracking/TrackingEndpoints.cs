using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Analytics;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Api.Tracking;

/// <summary>
/// Tracker ingest endpoints. Every endpoint is same-origin-gated (403 on foreign Origin)
/// and returns <c>204 NoContent</c> — the client never waits on analytics.
///
/// T-1:  <c>POST /api/tracking/ping</c>     — heartbeat
/// T-2:  <c>POST /api/tracking/pageview</c> — enriched pageview with UTM/click-ids/UA/viewport
/// T-3:  <c>POST /api/tracking/signals</c>  — per-session browser signals (idempotent)
///
/// Subsequent slices add <c>/clicks</c>, <c>/scroll</c>, <c>/mouse-trail</c>,
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

    private static bool HonorsPrivacySignal(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("DNT", out var dnt) && dnt.ToString() == "1") return true;
        if (context.Request.Headers.TryGetValue("Sec-GPC", out var gpc) && gpc.ToString() == "1") return true;
        return false;
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

    public sealed class SignalsRequest
    {
        public string? Vid { get; set; }
        public string? Sid { get; set; }
        public string? Timezone { get; set; }
        public string? Language { get; set; }
        public int? ColorDepth { get; set; }
        public int? HardwareConcurrency { get; set; }
        public int? DeviceMemory { get; set; }
        public int? TouchPoints { get; set; }
        public int? ScreenW { get; set; }
        public int? ScreenH { get; set; }
        public double? PixelRatio { get; set; }
        public bool? Cookies { get; set; }
        public bool? LocalStorage { get; set; }
        public bool? SessionStorage { get; set; }
        public bool? IsMetaWebview { get; set; }
        public bool? IsTikTokWebview { get; set; }
        public string? CanvasFingerprint { get; set; }
        public string? WebGLVendor { get; set; }
        public string? WebGLRenderer { get; set; }
        public string? Battery { get; set; }
        public string? Connection { get; set; }
    }

    public sealed class ClickItem
    {
        public string? Sid { get; set; }
        public string? Vid { get; set; }
        public string? Path { get; set; }
        public DateTime? Ts { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public int? ViewportW { get; set; }
        public int? ViewportH { get; set; }
        public int? PageW { get; set; }
        public int? PageH { get; set; }
        public string? Selector { get; set; }
        public string? TagName { get; set; }
        public string? ElementText { get; set; }
        public string? ElementHref { get; set; }
        public int? MouseButton { get; set; }
        public bool? IsDead { get; set; }
    }

    public sealed class ClicksRequest
    {
        public List<ClickItem>? Clicks { get; set; }
    }

    public const int MaxClicksPerSession = 500;

    public static void MapTrackingEndpoints(this WebApplication app)
    {
        // T-1 heartbeat — returns 204 and logs, AND bumps Session.EventCount.
        app.MapPost("/api/tracking/ping", async (
            PingRequest? request,
            HttpContext context,
            ISessionService sessions,
            IConfiguration config,
            ILogger<TrackingPingMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (HonorsPrivacySignal(context))
                return Results.StatusCode(StatusCodes.Status204NoContent);

            var vid = request?.Vid ?? "(anon)";
            var sid = request?.Sid ?? "(anon)";
            var path = request?.Path ?? "/";
            logger.LogInformation(
                "Tracking ping vid={Vid} sid={Sid} path={Path} consent={Consent}",
                vid, sid, path, request?.ConsentState ?? "none");

            var rawIp = context.Connection.RemoteIpAddress?.ToString();
            var salt = config["Analytics:IpHashSalt"] ?? "default-salt-change-in-prod";
            var ipHash = string.IsNullOrEmpty(rawIp) ? null : HashIp(rawIp, salt);

            await sessions.UpsertAsync(new UpsertInput(
                Vid: request?.Vid, Sid: request?.Sid, Path: request?.Path,
                UserAgent: context.Request.Headers.UserAgent.FirstOrDefault(),
                IpAddress: rawIp, Referrer: null,
                UtmSource: null, UtmMedium: null, UtmCampaign: null, UtmTerm: null, UtmContent: null,
                Gclid: null, Fbclid: null, Msclkid: null,
                ViewportW: null, ViewportH: null,
                ConsentState: request?.ConsentState,
                EventKind: EventKind.Ping,
                IpHash: ipHash));

            return Results.StatusCode(StatusCodes.Status204NoContent);
        });

        // T-2 enriched pageview. Writes one PageView row, flips LandingFlag, parses UA.
        // T-3: also upserts Session + Visitor with bot classification.
        app.MapPost("/api/tracking/pageview", async (
            PageviewRequest? request,
            HttpContext context,
            AppDbContext db,
            ISessionService sessions,
            IConfiguration config,
            ILogger<TrackingPageviewMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (request is null)
                return Results.StatusCode(StatusCodes.Status204NoContent);
            if (HonorsPrivacySignal(context))
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
                logger.LogWarning(ex, "Pageview persist failed for sid={Sid} path={Path}", sid, path);
            }

            await sessions.UpsertAsync(new UpsertInput(
                Vid: vid, Sid: sid, Path: path,
                UserAgent: ua, IpAddress: rawIp,
                Referrer: request.Referrer,
                UtmSource: request.UtmSource, UtmMedium: request.UtmMedium,
                UtmCampaign: request.UtmCampaign, UtmTerm: request.UtmTerm, UtmContent: request.UtmContent,
                Gclid: request.Gclid, Fbclid: request.Fbclid, Msclkid: request.Msclkid,
                ViewportW: request.ViewportW, ViewportH: request.ViewportH,
                ConsentState: request.ConsentState,
                EventKind: EventKind.Pageview,
                IpHash: ipHash));

            return Results.StatusCode(StatusCodes.Status204NoContent);
        });

        // T-3 signals — once per session. Idempotent: second post for the same sid
        // updates the existing row rather than inserting (unique index on SessionId).
        app.MapPost("/api/tracking/signals", async (
            SignalsRequest? request,
            HttpContext context,
            AppDbContext db,
            ISessionService sessions,
            IConfiguration config,
            ILogger<TrackingSignalsMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (request is null || string.IsNullOrWhiteSpace(request.Sid))
                return Results.StatusCode(StatusCodes.Status204NoContent);
            if (HonorsPrivacySignal(context))
                return Results.StatusCode(StatusCodes.Status204NoContent);

            var sid = request.Sid!;
            var rawIp = context.Connection.RemoteIpAddress?.ToString();
            var salt = config["Analytics:IpHashSalt"] ?? "default-salt-change-in-prod";
            var ipHash = string.IsNullOrEmpty(rawIp) ? null : HashIp(rawIp, salt);

            try
            {
                var existing = await db.BrowserSignals.FirstOrDefaultAsync(b => b.SessionId == sid);
                if (existing is null)
                {
                    db.BrowserSignals.Add(new BrowserSignal
                    {
                        SessionId = sid,
                        Timezone = request.Timezone,
                        Language = request.Language,
                        ColorDepth = request.ColorDepth,
                        HardwareConcurrency = request.HardwareConcurrency,
                        DeviceMemory = request.DeviceMemory,
                        TouchPoints = request.TouchPoints,
                        ScreenW = request.ScreenW,
                        ScreenH = request.ScreenH,
                        PixelRatio = request.PixelRatio,
                        Cookies = request.Cookies,
                        LocalStorage = request.LocalStorage,
                        SessionStorage = request.SessionStorage,
                        IsMetaWebview = request.IsMetaWebview,
                        IsTikTokWebview = request.IsTikTokWebview,
                        CanvasFingerprint = request.CanvasFingerprint,
                        WebGLVendor = request.WebGLVendor,
                        WebGLRenderer = request.WebGLRenderer,
                        Battery = request.Battery,
                        Connection = request.Connection,
                        CapturedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    // Idempotent merge — fill nulls, don't churn already-captured values.
                    existing.Timezone            ??= request.Timezone;
                    existing.Language            ??= request.Language;
                    existing.ColorDepth          ??= request.ColorDepth;
                    existing.HardwareConcurrency ??= request.HardwareConcurrency;
                    existing.DeviceMemory        ??= request.DeviceMemory;
                    existing.TouchPoints         ??= request.TouchPoints;
                    existing.ScreenW             ??= request.ScreenW;
                    existing.ScreenH             ??= request.ScreenH;
                    existing.PixelRatio          ??= request.PixelRatio;
                    existing.Cookies             ??= request.Cookies;
                    existing.LocalStorage        ??= request.LocalStorage;
                    existing.SessionStorage      ??= request.SessionStorage;
                    existing.IsMetaWebview       ??= request.IsMetaWebview;
                    existing.IsTikTokWebview     ??= request.IsTikTokWebview;
                    existing.CanvasFingerprint   ??= request.CanvasFingerprint;
                    existing.WebGLVendor         ??= request.WebGLVendor;
                    existing.WebGLRenderer       ??= request.WebGLRenderer;
                    existing.Battery             ??= request.Battery;
                    existing.Connection          ??= request.Connection;
                }
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Signals persist failed for sid={Sid}", sid);
            }

            // Also bump the session's event count so Health shows signals traffic.
            await sessions.UpsertAsync(new UpsertInput(
                Vid: request.Vid, Sid: request.Sid, Path: null,
                UserAgent: context.Request.Headers.UserAgent.FirstOrDefault(),
                IpAddress: rawIp, Referrer: null,
                UtmSource: null, UtmMedium: null, UtmCampaign: null, UtmTerm: null, UtmContent: null,
                Gclid: null, Fbclid: null, Msclkid: null,
                ViewportW: null, ViewportH: null,
                ConsentState: null,
                EventKind: EventKind.Signals,
                IpHash: ipHash));

            return Results.StatusCode(StatusCodes.Status204NoContent);
        });

        // T-4 clickstream batch — accepts an array of clicks. Server-side rage
        // detection: a click whose prior 2 on the same (sid, selector) are within
        // 500 ms of it triggers IsRage=true on the trailing 2 + this click.
        app.MapPost("/api/tracking/clicks", async (
            ClicksRequest? request,
            HttpContext context,
            AppDbContext db,
            ILogger<TrackingClicksMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (HonorsPrivacySignal(context))
                return Results.StatusCode(StatusCodes.Status204NoContent);
            if (request?.Clicks is null || request.Clicks.Count == 0)
                return Results.StatusCode(StatusCodes.Status204NoContent);

            try
            {
                // Group by session so we can enforce the per-session 500 cap cheaply.
                foreach (var byGroup in request.Clicks
                             .Where(c => !string.IsNullOrWhiteSpace(c.Sid) && !string.IsNullOrWhiteSpace(c.Selector))
                             .GroupBy(c => c.Sid!))
                {
                    var sid = byGroup.Key;
                    var existingCount = await db.ClickEvents.CountAsync(ce => ce.SessionId == sid);
                    var remaining = MaxClicksPerSession - existingCount;
                    if (remaining <= 0)
                    {
                        logger.LogInformation("Click cap reached for sid={Sid} — {Dropped} dropped", sid, byGroup.Count());
                        continue;
                    }

                    foreach (var item in byGroup.OrderBy(c => c.Ts ?? DateTime.UtcNow).Take(remaining))
                    {
                        var ts = item.Ts ?? DateTime.UtcNow;
                        var row = new ClickEvent
                        {
                            SessionId = sid,
                            VisitorId = item.Vid,
                            Path = string.IsNullOrWhiteSpace(item.Path) ? "/" : item.Path!,
                            Ts = ts,
                            X = item.X ?? 0,
                            Y = item.Y ?? 0,
                            ViewportW = item.ViewportW ?? 0,
                            ViewportH = item.ViewportH ?? 0,
                            PageW = item.PageW ?? 0,
                            PageH = item.PageH ?? 0,
                            Selector = Truncate(item.Selector!, 500) ?? string.Empty,
                            TagName = Truncate(item.TagName, 50),
                            ElementText = Truncate(item.ElementText, 64),
                            ElementHref = Truncate(item.ElementHref, 2000),
                            MouseButton = item.MouseButton ?? 0,
                            IsDead = item.IsDead ?? false,
                            IsRage = false
                        };

                        // Rage detection: the prior 2 clicks on the same (sid, selector)
                        // within 500ms must BOTH be present. Save per-row so in-batch rage
                        // bursts are visible to this query on the next iteration.
                        var windowStart = ts.AddMilliseconds(-500);
                        var priors = await db.ClickEvents
                            .Where(c => c.SessionId == sid
                                     && c.Selector == row.Selector
                                     && c.Ts >= windowStart
                                     && c.Ts <= ts)
                            .OrderByDescending(c => c.Ts)
                            .Take(2)
                            .ToListAsync();

                        if (priors.Count >= 2)
                        {
                            foreach (var p in priors) p.IsRage = true;
                            row.IsRage = true;
                        }

                        db.ClickEvents.Add(row);
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Clicks persist failed");
            }

            return Results.StatusCode(StatusCodes.Status204NoContent);
        });
    }

    private static string? Truncate(string? s, int max)
    {
        if (s is null) return null;
        return s.Length <= max ? s : s[..max];
    }

    /// <summary>ILogger category marker for ping.</summary>
    public sealed class TrackingPingMarker { }
    /// <summary>ILogger category marker for pageview.</summary>
    public sealed class TrackingPageviewMarker { }
    /// <summary>ILogger category marker for signals.</summary>
    public sealed class TrackingSignalsMarker { }
    /// <summary>ILogger category marker for clicks.</summary>
    public sealed class TrackingClicksMarker { }
}
