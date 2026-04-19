using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Lightweight anomaly detection. Reads from EventRollupDaily (written nightly
/// by RollupRunner) rather than raw event tables so detection survives T-10's
/// 90-day raw-event purge. Any |z-score| &gt; 2 against the 7-day trailing
/// mean becomes an Insight row.
/// </summary>
public interface IInsightsService
{
    Task<IReadOnlyList<Insight>> DetectAsync();
    Task<IReadOnlyList<Insight>> RecentAsync(int limit = 50);
}

public class InsightsService : IInsightsService
{
    private readonly AppDbContext _db;
    private readonly IRollupRunner _rollup;
    private readonly ILogger<InsightsService> _logger;

    public InsightsService(AppDbContext db, IRollupRunner rollup, ILogger<InsightsService> logger)
    {
        _db = db; _rollup = rollup; _logger = logger;
    }

    public async Task<IReadOnlyList<Insight>> DetectAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = today.AddDays(-7);

        // Make sure today's rollup exists — raw events are still fresh within 90d,
        // so rolling it up now gives us a today-row to compare against the baseline.
        try { await _rollup.RollupDayAsync(today); }
        catch (Exception ex) { _logger.LogDebug(ex, "InsightsService opportunistic rollup failed"); }

        var results = new List<Insight>();
        string[] metrics = { "pageviews", "sessions", "submissions", "errors" };

        foreach (var metric in metrics)
        {
            // Sum per day across all paths — we want the total-metric anomaly,
            // not per-path (per-path detection is a richer follow-up).
            var rows = await _db.EventRollupDailies
                .Where(r => r.Metric == metric
                         && r.Dimension == ""
                         && r.Date >= weekAgo
                         && r.Date <= today)
                .Select(r => new { r.Date, r.Value })
                .ToListAsync();

            var byDay = rows
                .GroupBy(r => r.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Value));

            var baseline = new List<long>();
            for (var d = weekAgo; d < today; d = d.AddDays(1))
            {
                baseline.Add(byDay.GetValueOrDefault(d.Date, 0));
            }
            var current = byDay.GetValueOrDefault(today, 0);
            if (baseline.Count < 3) continue; // not enough history yet

            var mean = baseline.Average();
            var stdev = baseline.Count > 1
                ? Math.Sqrt(baseline.Sum(x => Math.Pow(x - mean, 2)) / (baseline.Count - 1))
                : 0;
            if (stdev < 0.5) stdev = Math.Max(1, mean * 0.1);
            var z = stdev == 0 ? 0 : (current - mean) / stdev;
            if (Math.Abs(z) >= 2)
            {
                var direction = z > 0 ? "spike" : "drop";
                var severity = Math.Abs(z) >= 4 ? "critical" : Math.Abs(z) >= 3 ? "warn" : "info";
                results.Add(new Insight
                {
                    Title = $"{metric} {direction}: {current} vs 7d mean {Math.Round(mean, 1)}",
                    Metric = metric,
                    Score = z,
                    Current = current,
                    Baseline = mean,
                    Severity = severity,
                    CreatedAt = now
                });
            }
        }

        if (results.Count > 0)
        {
            _db.Insights.AddRange(results);
            await _db.SaveChangesAsync();
        }
        return results;
    }

    public async Task<IReadOnlyList<Insight>> RecentAsync(int limit = 50)
    {
        return await _db.Insights.Where(i => i.DismissedAt == null)
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}
