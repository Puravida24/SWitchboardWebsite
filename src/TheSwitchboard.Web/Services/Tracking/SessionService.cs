using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Upserts <see cref="Session"/> + <see cref="Visitor"/> rows on every tracker
/// event (ping, pageview, signals, …). The first event for a sid creates the
/// Session row; subsequent events bump PageCount / EventCount / EndedAt and
/// fill in any still-null device / attribution fields (first-wins for the
/// landing attribution, last-wins for EndedAt).
///
/// Bot classification runs once on first upsert — re-classifying on later
/// events would churn the column and mask bot-vs-human splits.
/// </summary>
public interface ISessionService
{
    Task UpsertAsync(UpsertInput input);
}

public sealed record UpsertInput(
    string? Vid,
    string? Sid,
    string? Path,
    string? UserAgent,
    string? IpAddress,
    string? Referrer,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    string? UtmTerm,
    string? UtmContent,
    string? Gclid,
    string? Fbclid,
    string? Msclkid,
    int? ViewportW,
    int? ViewportH,
    string? ConsentState,
    EventKind EventKind,
    string? IpHash);

public enum EventKind { Ping, Pageview, Signals }

public class SessionService : ISessionService
{
    private readonly AppDbContext _db;
    private readonly IIpClassificationService _ipClassifier;
    private readonly IRealtimeMetrics _metrics;
    private readonly IRealtimeBroadcaster _broadcaster;

    public SessionService(
        AppDbContext db,
        IIpClassificationService ipClassifier,
        IRealtimeMetrics metrics,
        IRealtimeBroadcaster broadcaster)
    {
        _db = db;
        _ipClassifier = ipClassifier;
        _metrics = metrics;
        _broadcaster = broadcaster;
    }

    public async Task UpsertAsync(UpsertInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Sid)) return; // need a session key

        var sid = input.Sid!;
        var vid = input.Vid;
        var now = DateTime.UtcNow;

        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == sid);
        if (session is null)
        {
            var parsed = UserAgentParser.Parse(input.UserAgent);
            var (isBot, botReason) = _ipClassifier.Classify(input.UserAgent, input.IpAddress);

            session = new Session
            {
                Id = sid,
                VisitorId = vid,
                StartedAt = now,
                EndedAt = now,
                PageCount = 0,
                EventCount = 0,
                LandingPath = input.Path,
                Referrer = input.Referrer,
                UtmSource = input.UtmSource,
                UtmMedium = input.UtmMedium,
                UtmCampaign = input.UtmCampaign,
                UtmTerm = input.UtmTerm,
                UtmContent = input.UtmContent,
                Gclid = input.Gclid,
                Fbclid = input.Fbclid,
                Msclkid = input.Msclkid,
                IpHash = input.IpHash,
                DeviceType = parsed.DeviceType,
                Browser = parsed.Browser,
                Os = parsed.Os,
                ViewportW = input.ViewportW,
                ViewportH = input.ViewportH,
                IsBot = isBot,
                BotReason = botReason,
                ConsentState = input.ConsentState ?? "none",
                Converted = false
            };
            _db.Sessions.Add(session);
        }
        else
        {
            session.EndedAt = now;
            session.DurationSeconds = (int)(now - session.StartedAt).TotalSeconds;
            session.ExitPath = input.Path ?? session.ExitPath;
            // First-touch wins for attribution — don't overwrite if already set.
            session.UtmSource  ??= input.UtmSource;
            session.UtmMedium  ??= input.UtmMedium;
            session.UtmCampaign??= input.UtmCampaign;
            session.UtmTerm    ??= input.UtmTerm;
            session.UtmContent ??= input.UtmContent;
            session.Gclid      ??= input.Gclid;
            session.Fbclid     ??= input.Fbclid;
            session.Msclkid    ??= input.Msclkid;
            // Fill device fields if not yet known (ping without UA then pageview with UA).
            if (string.IsNullOrEmpty(session.DeviceType) && !string.IsNullOrEmpty(input.UserAgent))
            {
                var parsed = UserAgentParser.Parse(input.UserAgent);
                session.DeviceType = parsed.DeviceType;
                session.Browser    = parsed.Browser;
                session.Os         = parsed.Os;
            }
            session.ViewportW ??= input.ViewportW;
            session.ViewportH ??= input.ViewportH;
        }

        session.EventCount += 1;
        if (input.EventKind == EventKind.Pageview) session.PageCount += 1;

        // Upsert Visitor alongside.
        if (!string.IsNullOrWhiteSpace(vid))
        {
            var visitor = await _db.Visitors.FirstOrDefaultAsync(v => v.Id == vid);
            if (visitor is null)
            {
                _db.Visitors.Add(new Visitor
                {
                    Id = vid!,
                    FirstSeen = now,
                    LastSeen = now,
                    SessionCount = 1
                });
            }
            else
            {
                visitor.LastSeen = now;
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Concurrent-request race on the same sid/vid — both threads added rows.
            // Reload both entities and replay the bumps. Catches Session AND Visitor
            // key collisions (the Playwright suite surfaced the Visitor variant when
            // tests ran in parallel — InMemory "an item with the same key has already
            // been added" at Sessions.SaveChangesAsync).
            _db.ChangeTracker.Clear();

            var existingSession = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == sid);
            if (existingSession is not null)
            {
                existingSession.EndedAt = now;
                existingSession.EventCount += 1;
                if (input.EventKind == EventKind.Pageview) existingSession.PageCount += 1;
            }

            if (!string.IsNullOrWhiteSpace(vid))
            {
                var existingVisitor = await _db.Visitors.FirstOrDefaultAsync(v => v.Id == vid);
                if (existingVisitor is not null)
                {
                    existingVisitor.LastSeen = now;
                }
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Race on the retry too (very rare — 3-way concurrency). Swallow:
                // the event is ephemeral tracking data, not transactional business
                // state, so dropping this one signal is preferable to propagating
                // a 5xx to the browser.
            }
        }

        // T-8: touch the live-visitor counter + fire a realtime broadcast.
        if (!string.IsNullOrWhiteSpace(vid)) _metrics.TouchVisitor(vid!, now);
        await _broadcaster.BroadcastActivityAsync(new ActivityEvent(
            Kind: input.EventKind.ToString().ToLowerInvariant(),
            Path: input.Path,
            VisitorId: vid,
            SessionId: sid,
            DeviceType: session.DeviceType,
            Browser: session.Browser,
            UtmSource: session.UtmSource,
            IsBot: session.IsBot,
            Ts: now));
    }
}
