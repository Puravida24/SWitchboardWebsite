using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Analytics;

namespace TheSwitchboard.Web.Pages.Admin;

public class AnalyticsModel : PageModel
{
    private readonly AppDbContext _db;

    public AnalyticsModel(AppDbContext db) { _db = db; }

    public int Today { get; private set; }
    public int Week { get; private set; }
    public int Month { get; private set; }
    public int UniqueVisitorsWeek { get; private set; }
    public List<PagePopularity> TopPages { get; private set; } = new();
    public Dictionary<int, int> ScrollBuckets { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var now = DateTime.UtcNow;
        var startOfDay = now.Date;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddDays(-30);

        Today = await _db.Set<PageView>().CountAsync(v => v.Timestamp >= startOfDay);
        Week = await _db.Set<PageView>().CountAsync(v => v.Timestamp >= weekAgo);
        Month = await _db.Set<PageView>().CountAsync(v => v.Timestamp >= monthAgo);

        // Client-side Distinct — InMemory provider doesn't translate Distinct() on strings.
        var hashes = await _db.Set<PageView>()
            .Where(v => v.Timestamp >= weekAgo && v.IpHash != null)
            .Select(v => v.IpHash!)
            .ToListAsync();
        UniqueVisitorsWeek = hashes.Distinct().Count();

        var recentPaths = await _db.Set<PageView>()
            .Where(v => v.Timestamp >= weekAgo)
            .Select(v => v.Path)
            .ToListAsync();
        TopPages = recentPaths
            .GroupBy(p => p)
            .Select(g => new PagePopularity(g.Key, g.Count()))
            .OrderByDescending(p => p.Count)
            .Take(10)
            .ToList();

        // Scroll bucket counts — read AnalyticsEvent rows with Name=="scroll_depth".
        var events = await _db.Set<AnalyticsEvent>()
            .Where(e => e.Name == "scroll_depth" && e.Timestamp >= weekAgo)
            .Select(e => e.Value)
            .ToListAsync();
        foreach (var v in events)
        {
            if (int.TryParse(v, out var pct))
            {
                var bucket = pct switch { >= 100 => 100, >= 75 => 75, >= 50 => 50, >= 25 => 25, _ => 0 };
                if (bucket > 0) ScrollBuckets[bucket] = ScrollBuckets.GetValueOrDefault(bucket, 0) + 1;
            }
        }
    }

    public record PagePopularity(string Path, int Count);
}
