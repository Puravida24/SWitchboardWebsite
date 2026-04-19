using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Analytics;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services;
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
    public const int MaxMouseTrailPerSession = 300;

    public sealed class ScrollItem
    {
        public string? Sid { get; set; }
        public string? Vid { get; set; }
        public string? Path { get; set; }
        public DateTime? Ts { get; set; }
        public int? Depth { get; set; }
        public int? MaxDepth { get; set; }
        public int? ViewportH { get; set; }
        public int? DocumentH { get; set; }
        public int? TimeSinceLoadMs { get; set; }
    }

    public sealed class ScrollBatch
    {
        public List<ScrollItem>? Samples { get; set; }
    }

    public sealed class MousePoint
    {
        public string? Sid { get; set; }
        public string? Vid { get; set; }
        public string? Path { get; set; }
        public DateTime? Ts { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public int? ViewportW { get; set; }
        public int? ViewportH { get; set; }
    }

    public sealed class MouseBatch
    {
        public List<MousePoint>? Points { get; set; }
    }

    public sealed class FormEventItem
    {
        public string? Sid { get; set; }
        public string? Vid { get; set; }
        public string? Path { get; set; }
        public string? FormId { get; set; }
        public string? FieldName { get; set; }
        public string? Event { get; set; }
        public DateTime? OccurredAt { get; set; }
        public int? DwellMs { get; set; }
        public int? CharCount { get; set; }
        public int? CorrectionCount { get; set; }
        public bool? PastedFlag { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public sealed class FormEventsBatch
    {
        public List<FormEventItem>? Events { get; set; }
    }

    public sealed class VitalItem
    {
        public string? Sid { get; set; }
        public string? Vid { get; set; }
        public string? Path { get; set; }
        public DateTime? Ts { get; set; }
        public string? Metric { get; set; }
        public double? Value { get; set; }
        public string? NavigationType { get; set; }
        public string? NavId { get; set; }
    }

    public sealed class VitalsBatch
    {
        public List<VitalItem>? Vitals { get; set; }
    }

    public sealed class JsErrorItem
    {
        public string? Sid { get; set; }
        public string? Vid { get; set; }
        public string? Path { get; set; }
        public DateTime? Ts { get; set; }
        public string? Message { get; set; }
        public string? Stack { get; set; }
        public string? Source { get; set; }
        public int? Line { get; set; }
        public int? Col { get; set; }
        public string? UserAgent { get; set; }
        public string? BuildId { get; set; }
    }

    public sealed class ErrorsBatch
    {
        public List<JsErrorItem>? Errors { get; set; }
    }

    public sealed class ReplayChunkRequest
    {
        public string? Sid { get; set; }
        public int? Sequence { get; set; }
        public DateTime? Ts { get; set; }
        public bool? Compressed { get; set; }
        /// <summary>Base64-encoded gzip bytes — pre-compressed client-side via CompressionStream.</summary>
        public string? PayloadBase64 { get; set; }
    }

    public const int MaxReplayChunkBytes = 512 * 1024;

    public sealed class ConsentRequest
    {
        public string? Sid { get; set; }
        public string? Vid { get; set; }
        public int? FormSubmissionId { get; set; }
        public DateTime? ConsentTimestamp { get; set; }
        public string? ConsentMethod { get; set; }
        public string? ConsentElementSelector { get; set; }
        public int? ClickX { get; set; }
        public int? ClickY { get; set; }
        public DateTime? PageLoadedAt { get; set; }
        public int? TimeOnPageSeconds { get; set; }
        public string? DisclosureText { get; set; }
        public string? DisclosureFontSize { get; set; }
        public string? DisclosureColor { get; set; }
        public string? DisclosureBackgroundColor { get; set; }
        public double? DisclosureContrastRatio { get; set; }
        public bool? DisclosureIsVisible { get; set; }
        public string? UserAgent { get; set; }
        public string? BrowserName { get; set; }
        public string? OsName { get; set; }
        public string? ScreenResolution { get; set; }
        public int? ViewportW { get; set; }
        public int? ViewportH { get; set; }
        public string? PageUrl { get; set; }
        public int? KeystrokesPerMinute { get; set; }
        public int? FormFieldsInteracted { get; set; }
        public int? MouseDistancePx { get; set; }
        public int? ScrollDepthPercent { get; set; }
        public string? EmailHashHex { get; set; }
        public string? PhoneHashHex { get; set; }
    }

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

        // T-5 scroll milestones + max-depth samples. Dedupes per (sid, path, depth)
        // via the unique index on ScrollSamples — duplicate POSTs are catch-and-ignore.
        app.MapPost("/api/tracking/scroll", async (
            ScrollBatch? request,
            HttpContext context,
            AppDbContext db,
            ILogger<TrackingScrollMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (HonorsPrivacySignal(context))
                return Results.StatusCode(StatusCodes.Status204NoContent);
            if (request?.Samples is null || request.Samples.Count == 0)
                return Results.StatusCode(StatusCodes.Status204NoContent);

            foreach (var s in request.Samples)
            {
                if (string.IsNullOrWhiteSpace(s.Sid) || s.Depth is null) continue;
                var path = string.IsNullOrWhiteSpace(s.Path) ? "/" : s.Path!;
                var depth = s.Depth.Value;

                try
                {
                    var exists = await db.ScrollSamples.AnyAsync(x =>
                        x.SessionId == s.Sid && x.Path == path && x.Depth == depth);
                    if (exists) continue;

                    db.ScrollSamples.Add(new ScrollSample
                    {
                        SessionId = s.Sid!,
                        Path = path,
                        Ts = s.Ts ?? DateTime.UtcNow,
                        Depth = depth,
                        MaxDepth = s.MaxDepth ?? depth,
                        ViewportH = s.ViewportH ?? 0,
                        DocumentH = s.DocumentH ?? 0,
                        TimeSinceLoadMs = s.TimeSinceLoadMs ?? 0
                    });
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Unique-index race — another request inserted the same milestone
                    // between our AnyAsync check and SaveChanges. Safe to ignore.
                    db.ChangeTracker.Clear();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Scroll persist failed for sid={Sid}", s.Sid);
                }
            }
            return Results.StatusCode(StatusCodes.Status204NoContent);
        });

        // T-5 mouse trail — sampled XY coords, capped 300/session.
        app.MapPost("/api/tracking/mouse-trail", async (
            MouseBatch? request,
            HttpContext context,
            AppDbContext db,
            ILogger<TrackingMouseMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (HonorsPrivacySignal(context))
                return Results.StatusCode(StatusCodes.Status204NoContent);
            if (request?.Points is null || request.Points.Count == 0)
                return Results.StatusCode(StatusCodes.Status204NoContent);

            try
            {
                foreach (var byGroup in request.Points
                             .Where(p => !string.IsNullOrWhiteSpace(p.Sid))
                             .GroupBy(p => p.Sid!))
                {
                    var sid = byGroup.Key;
                    var existing = await db.MouseTrails.CountAsync(m => m.SessionId == sid);
                    var remaining = MaxMouseTrailPerSession - existing;
                    if (remaining <= 0)
                    {
                        logger.LogInformation("Mouse-trail cap reached for sid={Sid}", sid);
                        continue;
                    }

                    foreach (var p in byGroup.OrderBy(p => p.Ts ?? DateTime.UtcNow).Take(remaining))
                    {
                        db.MouseTrails.Add(new MouseTrail
                        {
                            SessionId = sid,
                            Path = string.IsNullOrWhiteSpace(p.Path) ? "/" : p.Path!,
                            Ts = p.Ts ?? DateTime.UtcNow,
                            X = p.X ?? 0,
                            Y = p.Y ?? 0,
                            ViewportW = p.ViewportW ?? 0,
                            ViewportH = p.ViewportH ?? 0
                        });
                    }
                }
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Mouse-trail persist failed");
            }
            return Results.StatusCode(StatusCodes.Status204NoContent);
        });

        // T-5 form events — focus/blur/input/paste/error/submit/abandon. One row
        // per event; dwell/charCount/pastedFlag already aggregated client-side.
        app.MapPost("/api/tracking/form-events", async (
            FormEventsBatch? request,
            HttpContext context,
            AppDbContext db,
            ILogger<TrackingFormEventsMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (HonorsPrivacySignal(context))
                return Results.StatusCode(StatusCodes.Status204NoContent);
            if (request?.Events is null || request.Events.Count == 0)
                return Results.StatusCode(StatusCodes.Status204NoContent);

            try
            {
                foreach (var e in request.Events)
                {
                    if (string.IsNullOrWhiteSpace(e.Sid) ||
                        string.IsNullOrWhiteSpace(e.FormId) ||
                        string.IsNullOrWhiteSpace(e.FieldName) ||
                        string.IsNullOrWhiteSpace(e.Event) ||
                        !ValidFormEvents.Contains(e.Event!)) continue;

                    db.FormInteractions.Add(new FormInteraction
                    {
                        SessionId = e.Sid!,
                        VisitorId = e.Vid,
                        Path = string.IsNullOrWhiteSpace(e.Path) ? "/" : e.Path!,
                        FormId = Truncate(e.FormId, 64) ?? "unknown",
                        FieldName = Truncate(e.FieldName, 64) ?? "unknown",
                        Event = e.Event!.ToLowerInvariant(),
                        OccurredAt = e.OccurredAt ?? DateTime.UtcNow,
                        DwellMs = e.DwellMs,
                        CharCount = e.CharCount,
                        CorrectionCount = e.CorrectionCount,
                        PastedFlag = e.PastedFlag,
                        ErrorCode = Truncate(e.ErrorCode, 50),
                        ErrorMessage = Truncate(e.ErrorMessage, 200)
                    });
                }
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Form events persist failed");
            }
            return Results.StatusCode(StatusCodes.Status204NoContent);
        });

        // T-6 web vitals — server rates the value against Google thresholds and
        // stores the bucket alongside the raw number. Thresholds are the public
        // web.dev rating table (LCP 2500/4000, CLS 0.1/0.25, INP 200/500, etc.).
        app.MapPost("/api/tracking/vitals", async (
            VitalsBatch? request,
            HttpContext context,
            AppDbContext db,
            ILogger<TrackingVitalsMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (HonorsPrivacySignal(context))
                return Results.StatusCode(StatusCodes.Status204NoContent);
            if (request?.Vitals is null || request.Vitals.Count == 0)
                return Results.StatusCode(StatusCodes.Status204NoContent);

            try
            {
                foreach (var v in request.Vitals)
                {
                    if (string.IsNullOrWhiteSpace(v.Sid) ||
                        string.IsNullOrWhiteSpace(v.Metric) ||
                        v.Value is null) continue;

                    db.WebVitalSamples.Add(new WebVitalSample
                    {
                        SessionId = v.Sid!,
                        Path = string.IsNullOrWhiteSpace(v.Path) ? "/" : v.Path!,
                        Ts = v.Ts ?? DateTime.UtcNow,
                        Metric = v.Metric!.ToUpperInvariant(),
                        Value = v.Value.Value,
                        Rating = RateVital(v.Metric!, v.Value.Value),
                        NavigationType = Truncate(v.NavigationType, 20),
                        NavId = Truncate(v.NavId, 64)
                    });
                }
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Vitals persist failed");
            }
            return Results.StatusCode(StatusCodes.Status204NoContent);
        });

        // T-6 JS errors — dedupe by fingerprint = sha256(message + source + line)[..16].
        // Same-fingerprint + same-session bumps Count; new session gets its own row
        // so conversion-impact correlation stays per-session.
        app.MapPost("/api/tracking/errors", async (
            ErrorsBatch? request,
            HttpContext context,
            AppDbContext db,
            ILogger<TrackingErrorsMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (HonorsPrivacySignal(context))
                return Results.StatusCode(StatusCodes.Status204NoContent);
            if (request?.Errors is null || request.Errors.Count == 0)
                return Results.StatusCode(StatusCodes.Status204NoContent);

            foreach (var e in request.Errors)
            {
                if (string.IsNullOrWhiteSpace(e.Sid) || string.IsNullOrWhiteSpace(e.Message)) continue;

                var fingerprint = Fingerprint($"{e.Message}|{e.Source ?? ""}|{e.Line ?? 0}");
                var now = e.Ts ?? DateTime.UtcNow;

                try
                {
                    var existing = await db.JsErrors.FirstOrDefaultAsync(j =>
                        j.SessionId == e.Sid && j.Fingerprint == fingerprint);

                    if (existing is not null)
                    {
                        existing.Count += 1;
                        existing.LastSeenAt = now;
                    }
                    else
                    {
                        db.JsErrors.Add(new JsError
                        {
                            SessionId = e.Sid!,
                            Path = string.IsNullOrWhiteSpace(e.Path) ? "/" : e.Path!,
                            Ts = now,
                            LastSeenAt = now,
                            Message = Truncate(e.Message, 500) ?? string.Empty,
                            StackRedacted = Truncate(PiiRedactor.RedactStack(e.Stack), 4000),
                            Source = Truncate(e.Source, 500),
                            Line = e.Line,
                            Col = e.Col,
                            UserAgent = Truncate(e.UserAgent, 500),
                            BuildId = Truncate(e.BuildId, 20),
                            Fingerprint = fingerprint,
                            Count = 1
                        });
                    }
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    db.ChangeTracker.Clear();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error persist failed for sid={Sid}", e.Sid);
                }
            }
            return Results.StatusCode(StatusCodes.Status204NoContent);
        });

        // T-7 replay chunk — gzipped rrweb event bytes, base64 in JSON. First
        // chunk for a sid creates the Replay envelope; subsequent chunks append.
        // Enforces 512 KB payload cap — oversized requests return 413.
        app.MapPost("/api/tracking/replay/chunk", async (
            ReplayChunkRequest? request,
            HttpContext context,
            AppDbContext db,
            ILogger<TrackingReplayMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (HonorsPrivacySignal(context))
                return Results.StatusCode(StatusCodes.Status204NoContent);
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Sid) ||
                string.IsNullOrEmpty(request.PayloadBase64))
                return Results.StatusCode(StatusCodes.Status204NoContent);

            byte[] bytes;
            try { bytes = Convert.FromBase64String(request.PayloadBase64!); }
            catch { return Results.StatusCode(StatusCodes.Status400BadRequest); }

            if (bytes.Length > MaxReplayChunkBytes)
            {
                logger.LogInformation("Replay chunk too large sid={Sid} size={Size}", request.Sid, bytes.Length);
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            var sid = request.Sid!;
            var now = DateTime.UtcNow;
            var ts = request.Ts ?? now;

            try
            {
                var replay = await db.Replays.FirstOrDefaultAsync(r => r.SessionId == sid);
                if (replay is null)
                {
                    replay = new Replay
                    {
                        SessionId = sid,
                        StartedAt = ts,
                        EndedAt = ts,
                        ChunkCount = 0,
                        ByteSize = 0,
                        Compressed = request.Compressed ?? true
                    };
                    db.Replays.Add(replay);
                    await db.SaveChangesAsync();
                }

                replay.ChunkCount += 1;
                replay.ByteSize += bytes.Length;
                replay.EndedAt = ts;
                replay.DurationSeconds = (int)(replay.EndedAt - replay.StartedAt).TotalSeconds;

                db.ReplayChunks.Add(new ReplayChunk
                {
                    ReplayId = replay.Id,
                    Sequence = request.Sequence ?? replay.ChunkCount - 1,
                    Ts = ts,
                    Payload = bytes
                });
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Replay chunk persist failed for sid={Sid}", sid);
            }

            return Results.StatusCode(StatusCodes.Status204NoContent);
        });

        // T-7B TCPA consent — captures the disclosure + behavioral signals at submit
        // time and returns a "sw_cert_…" id the client then stamps onto the form
        // submission. Unique text-hash auto-creates a DisclosureVersion row.
        app.MapPost("/api/tracking/consent", async (
            ConsentRequest? request,
            HttpContext context,
            AppDbContext db,
            ILogger<TrackingConsentMarker> logger) =>
        {
            if (!IsOriginAllowed(context))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (request is null || string.IsNullOrWhiteSpace(request.DisclosureText))
                return Results.BadRequest(new { error = "disclosureText required" });

            var now = DateTime.UtcNow;
            var rawIp = context.Connection.RemoteIpAddress?.ToString();

            // Server-side SHA-256 of the disclosure text — canonical hash, don't
            // trust the client to compute this.
            var disclosureHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(request.DisclosureText!))).ToLowerInvariant();

            // Upsert DisclosureVersion — new hash → auto-detected row.
            var version = await db.DisclosureVersions.FirstOrDefaultAsync(v => v.TextHash == disclosureHash);
            if (version is null)
            {
                var nextVersion = (await db.DisclosureVersions.CountAsync()) + 1;
                version = new DisclosureVersion
                {
                    Version = $"v{nextVersion}",
                    TextHash = disclosureHash,
                    FullText = Truncate(request.DisclosureText, 2000) ?? string.Empty,
                    EffectiveFrom = now,
                    Status = "auto-detected",
                    CreatedAt = now
                };
                try
                {
                    db.DisclosureVersions.Add(version);
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    db.ChangeTracker.Clear();
                    version = await db.DisclosureVersions.FirstAsync(v => v.TextHash == disclosureHash);
                }
            }

            // Bot heuristic — zero keystrokes + zero mouse distance + zero scroll
            // is the "scripted POST" signature.
            var isBot = (request.KeystrokesPerMinute ?? 0) == 0
                        && (request.MouseDistancePx ?? 0) == 0
                        && (request.ScrollDepthPercent ?? 0) == 0;

            var certId = "sw_cert_" + RandomBase36(24);

            var cert = new ConsentCertificate
            {
                CertificateId = certId,
                FormSubmissionId = request.FormSubmissionId,
                SessionId = request.Sid,
                ConsentTimestamp = request.ConsentTimestamp ?? now,
                ConsentMethod = Truncate(request.ConsentMethod, 40),
                ConsentElementSelector = Truncate(request.ConsentElementSelector, 500),
                ClickX = request.ClickX,
                ClickY = request.ClickY,
                PageLoadedAt = request.PageLoadedAt,
                TimeOnPageSeconds = request.TimeOnPageSeconds,
                DisclosureText = Truncate(request.DisclosureText, 2000) ?? string.Empty,
                DisclosureTextHash = disclosureHash,
                DisclosureVersionId = version.Id,
                DisclosureFontSize = Truncate(request.DisclosureFontSize, 20),
                DisclosureColor = Truncate(request.DisclosureColor, 30),
                DisclosureBackgroundColor = Truncate(request.DisclosureBackgroundColor, 30),
                DisclosureContrastRatio = request.DisclosureContrastRatio,
                DisclosureIsVisible = request.DisclosureIsVisible ?? false,
                IpAddress = Truncate(rawIp, 64),
                UserAgent = Truncate(request.UserAgent ?? context.Request.Headers.UserAgent.FirstOrDefault(), 500),
                BrowserName = Truncate(request.BrowserName, 50),
                OsName = Truncate(request.OsName, 50),
                ScreenResolution = Truncate(request.ScreenResolution, 30),
                ViewportW = request.ViewportW,
                ViewportH = request.ViewportH,
                PageUrl = Truncate(request.PageUrl, 2000),
                KeystrokesPerMinute = request.KeystrokesPerMinute,
                FormFieldsInteracted = request.FormFieldsInteracted,
                MouseDistancePx = request.MouseDistancePx,
                ScrollDepthPercent = request.ScrollDepthPercent,
                IsSuspiciousBot = isBot,
                EmailHash = Truncate(request.EmailHashHex, 64),
                PhoneHash = Truncate(request.PhoneHashHex, 64),
                CreatedAt = now,
                ExpiresAt = now.AddYears(5)
            };

            try
            {
                db.ConsentCertificates.Add(cert);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Consent certificate persist failed");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Results.Json(new { certificateId = certId });
        });
    }

    private static readonly char[] Base36Alphabet =
        "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    private static string RandomBase36(int len)
    {
        var bytes = new byte[len];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var chars = new char[len];
        for (var i = 0; i < len; i++) chars[i] = Base36Alphabet[bytes[i] % Base36Alphabet.Length];
        return new string(chars);
    }

    private static string RateVital(string metric, double value)
    {
        // Public web.dev thresholds — good | ni (needs-improvement) | poor.
        return metric.ToUpperInvariant() switch
        {
            "LCP"  => value <= 2500  ? "good" : value <= 4000  ? "ni" : "poor",
            "FCP"  => value <= 1800  ? "good" : value <= 3000  ? "ni" : "poor",
            "CLS"  => value <= 0.1   ? "good" : value <= 0.25  ? "ni" : "poor",
            "INP"  => value <= 200   ? "good" : value <= 500   ? "ni" : "poor",
            "TTFB" => value <= 800   ? "good" : value <= 1800  ? "ni" : "poor",
            _      => "good"
        };
    }

    private static string Fingerprint(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private static string? Truncate(string? s, int max)
    {
        if (s is null) return null;
        return s.Length <= max ? s : s[..max];
    }

    private static readonly HashSet<string> ValidFormEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "focus", "blur", "input", "paste", "error", "submit", "abandon"
    };

    /// <summary>ILogger category marker for ping.</summary>
    public sealed class TrackingPingMarker { }
    /// <summary>ILogger category marker for pageview.</summary>
    public sealed class TrackingPageviewMarker { }
    /// <summary>ILogger category marker for signals.</summary>
    public sealed class TrackingSignalsMarker { }
    /// <summary>ILogger category marker for clicks.</summary>
    public sealed class TrackingClicksMarker { }
    /// <summary>ILogger category marker for scroll.</summary>
    public sealed class TrackingScrollMarker { }
    /// <summary>ILogger category marker for mouse-trail.</summary>
    public sealed class TrackingMouseMarker { }
    /// <summary>ILogger category marker for form events.</summary>
    public sealed class TrackingFormEventsMarker { }
    /// <summary>ILogger category marker for vitals.</summary>
    public sealed class TrackingVitalsMarker { }
    /// <summary>ILogger category marker for errors.</summary>
    public sealed class TrackingErrorsMarker { }
    /// <summary>ILogger category marker for replay.</summary>
    public sealed class TrackingReplayMarker { }
    /// <summary>ILogger category marker for consent.</summary>
    public sealed class TrackingConsentMarker { }
}
