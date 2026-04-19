using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Analytics;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Drives /Admin/Reports/Attribution. Groups PageView rows by UTM source/medium/campaign,
/// restricted to landing pageviews (the first PV in a session) so each session counts exactly
/// once. Joins against sessions via SessionId.
/// </summary>
public interface IAttributionAnalyticsService
{
    Task<IReadOnlyList<AttributionBreakdown>> GetBreakdownAsync(DateTime fromUtc, DateTime toUtc, int limit = 50);
    Task<AttributionSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc);
    ParsedAttribution ParsePreview(string url);
}

public sealed record AttributionBreakdown(
    string Source,
    string Medium,
    string Campaign,
    int Sessions,
    int PageViews,
    int Bots);

public sealed record AttributionSummary(
    int TotalSessions,
    int SessionsWithAttribution,
    int GoogleAds,
    int MetaAds,
    int MicrosoftAds,
    int DirectOrOrganic);

/// <summary>Result shape of <see cref="IAttributionAnalyticsService.ParsePreview"/>.</summary>
public sealed record ParsedAttribution(
    string? UtmSource, string? UtmMedium, string? UtmCampaign, string? UtmTerm, string? UtmContent,
    string? Gclid, string? Fbclid, string? Msclkid);

public class AttributionAnalyticsService : IAttributionAnalyticsService
{
    private readonly AppDbContext _db;

    public AttributionAnalyticsService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<AttributionBreakdown>> GetBreakdownAsync(DateTime fromUtc, DateTime toUtc, int limit = 50)
    {
        // Landing PVs only — one row per session — so session counts are accurate.
        var rows = await _db.PageViews
            .Where(p => p.LandingFlag && p.Timestamp >= fromUtc && p.Timestamp <= toUtc)
            .Select(p => new
            {
                Source = p.UtmSource ?? "(direct)",
                Medium = p.UtmMedium ?? "(none)",
                Campaign = p.UtmCampaign ?? "(none)",
                SessionId = p.SessionId ?? string.Empty
            })
            .ToListAsync();

        // All PVs in window so we can include a PV count per attribution bucket too.
        var allPvs = await _db.PageViews
            .Where(p => p.Timestamp >= fromUtc && p.Timestamp <= toUtc)
            .Select(p => new
            {
                Source = p.UtmSource ?? "(direct)",
                Medium = p.UtmMedium ?? "(none)",
                Campaign = p.UtmCampaign ?? "(none)"
            })
            .ToListAsync();

        var pvByKey = allPvs
            .GroupBy(r => (r.Source, r.Medium, r.Campaign))
            .ToDictionary(g => g.Key, g => g.Count());

        return rows
            .GroupBy(r => (r.Source, r.Medium, r.Campaign))
            .Select(g => new AttributionBreakdown(
                g.Key.Source,
                g.Key.Medium,
                g.Key.Campaign,
                Sessions: g.Select(x => x.SessionId).Distinct().Count(),
                PageViews: pvByKey.GetValueOrDefault(g.Key, 0),
                Bots: 0))
            .OrderByDescending(b => b.Sessions)
            .Take(limit)
            .ToList();
    }

    public async Task<AttributionSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc)
    {
        var landings = await _db.PageViews
            .Where(p => p.LandingFlag && p.Timestamp >= fromUtc && p.Timestamp <= toUtc)
            .Select(p => new { p.UtmSource, p.Gclid, p.Fbclid, p.Msclkid, p.SessionId })
            .ToListAsync();

        var total = landings.Count;
        var attributed = landings.Count(l =>
            !string.IsNullOrEmpty(l.UtmSource) ||
            !string.IsNullOrEmpty(l.Gclid) ||
            !string.IsNullOrEmpty(l.Fbclid) ||
            !string.IsNullOrEmpty(l.Msclkid));
        var google = landings.Count(l => !string.IsNullOrEmpty(l.Gclid));
        var meta = landings.Count(l => !string.IsNullOrEmpty(l.Fbclid));
        var ms = landings.Count(l => !string.IsNullOrEmpty(l.Msclkid));
        var direct = total - attributed;

        return new AttributionSummary(total, attributed, google, meta, ms, direct);
    }

    public ParsedAttribution ParsePreview(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new(null, null, null, null, null, null, null, null);

        string? Get(string key)
        {
            try
            {
                // Allow bare "?utm_source=…" too.
                var qs = url.Contains('?') ? url[(url.IndexOf('?') + 1)..] : url;
                foreach (var pair in qs.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var eq = pair.IndexOf('=');
                    if (eq <= 0) continue;
                    var k = Uri.UnescapeDataString(pair[..eq]);
                    var v = Uri.UnescapeDataString(pair[(eq + 1)..]);
                    if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                        return v;
                }
            }
            catch { /* defensive */ }
            return null;
        }

        return new(
            Get("utm_source"), Get("utm_medium"), Get("utm_campaign"),
            Get("utm_term"), Get("utm_content"),
            Get("gclid"), Get("fbclid"), Get("msclkid"));
    }
}
