using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Lightweight anomaly detection. Walks key metrics (pageviews, submissions,
/// errors) for the current day against a 7-day trailing mean + stdev; any
/// absolute z-score &gt; 2 becomes an Insight row.
/// </summary>
public interface IInsightsService
{
    Task<IReadOnlyList<Insight>> DetectAsync();
    Task<IReadOnlyList<Insight>> RecentAsync(int limit = 50);
}

public class InsightsService : IInsightsService
{
    private readonly AppDbContext _db;
    private readonly ILogger<InsightsService> _logger;

    public InsightsService(AppDbContext db, ILogger<InsightsService> logger) { _db = db; _logger = logger; }

    public async Task<IReadOnlyList<Insight>> DetectAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = today.AddDays(-7);

        var results = new List<Insight>();

        async Task<int[]> BucketDays(DateTime from, DateTime to, int days, Func<DateTime, Task<int>> count)
        {
            var buckets = new int[days];
            for (var i = 0; i < days; i++)
            {
                buckets[i] = await count(from.AddDays(i));
            }
            return buckets;
        }

        async Task Detect(string metric, Func<DateTime, Task<int>> countDay)
        {
            var baseline = await BucketDays(weekAgo, today, 7, countDay);
            var current = await countDay(today);
            var mean = baseline.Average();
            var stdev = baseline.Length > 1
                ? Math.Sqrt(baseline.Sum(x => Math.Pow(x - mean, 2)) / (baseline.Length - 1))
                : 0;
            if (stdev < 0.5) stdev = Math.Max(1, mean * 0.1); // floor for near-flat baselines
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

        await Detect("pageviews", async d => await _db.PageViews.CountAsync(p => p.Timestamp >= d && p.Timestamp < d.AddDays(1)));
        await Detect("sessions",  async d => await _db.Sessions.CountAsync(s => s.StartedAt >= d && s.StartedAt < d.AddDays(1) && !s.IsBot));
        await Detect("submissions", async d => await _db.FormSubmissions.CountAsync(s => s.CreatedAt >= d && s.CreatedAt < d.AddDays(1)));
        await Detect("errors", async d => await _db.JsErrors.CountAsync(e => e.Ts >= d && e.Ts < d.AddDays(1)));

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
