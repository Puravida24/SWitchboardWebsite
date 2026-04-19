using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// /Admin/Reports/Overview — single composite health report.
/// HealthScore = weighted avg of Web Vitals good-rating share, inverse JS error
/// rate, and inverse bounce (single-PV session) rate. 0..100, higher is better.
/// </summary>
public interface IOverviewService
{
    Task<OverviewReport> GetAsync(DateTime fromUtc, DateTime toUtc);
}

public sealed record OverviewReport(
    int HealthScore,
    int TotalSessions,
    int TotalSubmissions,
    int TotalErrors,
    int VitalsGoodPct,
    int BouncePct,
    IReadOnlyList<DailyBucket> DailySeries,
    IReadOnlyList<TopRow> TopPages,
    IReadOnlyList<TopRow> TopCampaigns,
    IReadOnlyList<TopRow> TopErrors);

public sealed record DailyBucket(DateTime Date, int Sessions, int Submissions, int Errors);
public sealed record TopRow(string Label, int Count);

public class OverviewService : IOverviewService
{
    private readonly AppDbContext _db;
    public OverviewService(AppDbContext db) { _db = db; }

    public async Task<OverviewReport> GetAsync(DateTime fromUtc, DateTime toUtc)
    {
        var sessions = await _db.Sessions
            .Where(s => s.StartedAt >= fromUtc && s.StartedAt <= toUtc && !s.IsBot)
            .Select(s => new { s.StartedAt, s.PageCount, s.UtmSource })
            .ToListAsync();
        var submissions = await _db.FormSubmissions
            .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt <= toUtc)
            .Select(s => new { s.CreatedAt })
            .ToListAsync();
        var errors = await _db.JsErrors
            .Where(e => e.Ts >= fromUtc && e.Ts <= toUtc)
            .Select(e => new { e.Ts, e.Message })
            .ToListAsync();
        var vitals = await _db.WebVitalSamples
            .Where(v => v.Ts >= fromUtc && v.Ts <= toUtc)
            .Select(v => new { v.Rating })
            .ToListAsync();

        var totalSessions = sessions.Count;
        var bounces = sessions.Count(s => s.PageCount <= 1);
        var bouncePct = totalSessions == 0 ? 0 : (int)Math.Round(100.0 * bounces / totalSessions);
        var vitalsGoodPct = vitals.Count == 0 ? 100 : (int)Math.Round(100.0 * vitals.Count(v => v.Rating == "good") / vitals.Count);

        // Health = 60% vitals + 20% inverse-bounce + 20% inverse-error-per-session.
        var errorRatePct = totalSessions == 0 ? 0 : (int)Math.Round(100.0 * Math.Min(totalSessions, errors.Count) / totalSessions);
        var inverseBounce = 100 - bouncePct;
        var inverseErrors = 100 - errorRatePct;
        var health = (int)Math.Round(0.6 * vitalsGoodPct + 0.2 * inverseBounce + 0.2 * inverseErrors);
        health = Math.Clamp(health, 0, 100);

        // Daily series: last N days.
        var days = (int)Math.Ceiling((toUtc - fromUtc).TotalDays);
        days = Math.Max(1, Math.Min(days, 90));
        var series = new List<DailyBucket>(days);
        for (var i = days - 1; i >= 0; i--)
        {
            var dayStart = toUtc.Date.AddDays(-i);
            var dayEnd = dayStart.AddDays(1);
            series.Add(new DailyBucket(
                Date: dayStart,
                Sessions: sessions.Count(s => s.StartedAt >= dayStart && s.StartedAt < dayEnd),
                Submissions: submissions.Count(s => s.CreatedAt >= dayStart && s.CreatedAt < dayEnd),
                Errors: errors.Count(e => e.Ts >= dayStart && e.Ts < dayEnd)));
        }

        var allPaths = await _db.PageViews
            .Where(p => p.Timestamp >= fromUtc && p.Timestamp <= toUtc)
            .Select(p => p.Path)
            .ToListAsync();
        var topPages = allPaths
            .GroupBy(p => p)
            .Select(g => new TopRow(g.Key, g.Count()))
            .OrderByDescending(r => r.Count)
            .Take(5)
            .ToList();

        var topCampaigns = sessions
            .Where(s => !string.IsNullOrEmpty(s.UtmSource))
            .GroupBy(s => s.UtmSource!)
            .Select(g => new TopRow(g.Key, g.Count()))
            .OrderByDescending(r => r.Count)
            .Take(5)
            .ToList();

        var topErrors = errors
            .GroupBy(e => e.Message)
            .Select(g => new TopRow(g.Key.Length > 60 ? g.Key[..60] + "…" : g.Key, g.Count()))
            .OrderByDescending(r => r.Count)
            .Take(5)
            .ToList();

        return new OverviewReport(
            HealthScore: health,
            TotalSessions: totalSessions,
            TotalSubmissions: submissions.Count,
            TotalErrors: errors.Count,
            VitalsGoodPct: vitalsGoodPct,
            BouncePct: bouncePct,
            DailySeries: series,
            TopPages: topPages,
            TopCampaigns: topCampaigns,
            TopErrors: topErrors);
    }
}
