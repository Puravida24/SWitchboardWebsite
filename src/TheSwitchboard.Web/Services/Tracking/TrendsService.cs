using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// /Admin/Reports/Trends — pick a metric (sessions / pageviews / submissions /
/// errors / conversions), resolve a daily series for the chosen window, and
/// stack a prior-period comparison alongside.
/// </summary>
public interface ITrendsService
{
    Task<TrendSeries> SeriesAsync(string metric, DateTime fromUtc, DateTime toUtc);
}

public sealed record TrendSeries(
    string Metric,
    IReadOnlyList<DateTime> Days,
    IReadOnlyList<int> Current,
    IReadOnlyList<int> Previous,
    int CurrentTotal,
    int PreviousTotal,
    int ChangePct);

public class TrendsService : ITrendsService
{
    private readonly AppDbContext _db;
    public TrendsService(AppDbContext db) { _db = db; }

    public async Task<TrendSeries> SeriesAsync(string metric, DateTime fromUtc, DateTime toUtc)
    {
        var days = Math.Max(1, (int)Math.Ceiling((toUtc.Date - fromUtc.Date).TotalDays));
        var priorFrom = fromUtc.AddDays(-days);
        var priorTo = fromUtc;

        var current = await LoadBucketsAsync(metric, fromUtc, toUtc, days);
        var previous = await LoadBucketsAsync(metric, priorFrom, priorTo, days);

        var labels = new List<DateTime>(days);
        for (var i = 0; i < days; i++) labels.Add(fromUtc.Date.AddDays(i));

        var currentTotal = current.Sum();
        var previousTotal = previous.Sum();
        var changePct = previousTotal == 0
            ? (currentTotal > 0 ? 100 : 0)
            : (int)Math.Round(100.0 * (currentTotal - previousTotal) / previousTotal);

        return new TrendSeries(metric, labels, current, previous, currentTotal, previousTotal, changePct);
    }

    private async Task<int[]> LoadBucketsAsync(string metric, DateTime from, DateTime to, int days)
    {
        var buckets = new int[days];
        switch (metric.ToLowerInvariant())
        {
            case "sessions":
                var sess = await _db.Sessions
                    .Where(s => s.StartedAt >= from && s.StartedAt < to && !s.IsBot)
                    .Select(s => s.StartedAt)
                    .ToListAsync();
                foreach (var d in sess) Bump(buckets, from, d);
                break;
            case "submissions":
                var subs = await _db.FormSubmissions
                    .Where(s => s.CreatedAt >= from && s.CreatedAt < to)
                    .Select(s => s.CreatedAt)
                    .ToListAsync();
                foreach (var d in subs) Bump(buckets, from, d);
                break;
            case "errors":
                var errs = await _db.JsErrors
                    .Where(e => e.Ts >= from && e.Ts < to)
                    .Select(e => e.Ts)
                    .ToListAsync();
                foreach (var d in errs) Bump(buckets, from, d);
                break;
            case "conversions":
                var convs = await _db.Sessions
                    .Where(s => s.Converted && s.StartedAt >= from && s.StartedAt < to)
                    .Select(s => s.StartedAt)
                    .ToListAsync();
                foreach (var d in convs) Bump(buckets, from, d);
                break;
            case "pageviews":
            default:
                var pvs = await _db.PageViews
                    .Where(p => p.Timestamp >= from && p.Timestamp < to)
                    .Select(p => p.Timestamp)
                    .ToListAsync();
                foreach (var d in pvs) Bump(buckets, from, d);
                break;
        }
        return buckets;
    }

    private static void Bump(int[] buckets, DateTime from, DateTime d)
    {
        var idx = (int)(d.Date - from.Date).TotalDays;
        if (idx >= 0 && idx < buckets.Length) buckets[idx]++;
    }
}
