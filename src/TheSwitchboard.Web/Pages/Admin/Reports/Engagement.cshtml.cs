using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class EngagementModel : PageModel
{
    private readonly AppDbContext _db;
    public EngagementModel(AppDbContext db) { _db = db; }

    public int WindowDays { get; private set; } = 30;
    public int TotalSessions { get; private set; }
    public double AvgDurationSec { get; private set; }
    public double AvgPagesPerSession { get; private set; }
    public int ReturnVisitorPct { get; private set; }
    public IReadOnlyList<DurationBucket> DurationBuckets { get; private set; } = Array.Empty<DurationBucket>();

    public async Task OnGetAsync(int? days)
    {
        WindowDays = days is >= 1 and <= 180 ? days.Value : 30;
        var from = DateTime.UtcNow.AddDays(-WindowDays);

        var sessions = await _db.Sessions
            .Where(s => s.StartedAt >= from && !s.IsBot)
            .Select(s => new { s.DurationSeconds, s.PageCount, s.VisitorId })
            .ToListAsync();

        TotalSessions = sessions.Count;
        AvgDurationSec = sessions.Count == 0 ? 0 : sessions.Average(s => s.DurationSeconds);
        AvgPagesPerSession = sessions.Count == 0 ? 0 : sessions.Average(s => s.PageCount);

        var returners = sessions
            .Where(s => !string.IsNullOrEmpty(s.VisitorId))
            .GroupBy(s => s.VisitorId)
            .Count(g => g.Count() > 1);
        var uniqueVisitors = sessions
            .Where(s => !string.IsNullOrEmpty(s.VisitorId))
            .Select(s => s.VisitorId)
            .Distinct()
            .Count();
        ReturnVisitorPct = uniqueVisitors == 0 ? 0 : (int)Math.Round(100.0 * returners / uniqueVisitors);

        DurationBuckets = new[]
        {
            new DurationBucket("<10s",   sessions.Count(s => s.DurationSeconds < 10)),
            new DurationBucket("10-30s", sessions.Count(s => s.DurationSeconds >= 10 && s.DurationSeconds < 30)),
            new DurationBucket("30s-1m", sessions.Count(s => s.DurationSeconds >= 30 && s.DurationSeconds < 60)),
            new DurationBucket("1-3m",   sessions.Count(s => s.DurationSeconds >= 60 && s.DurationSeconds < 180)),
            new DurationBucket("3-10m",  sessions.Count(s => s.DurationSeconds >= 180 && s.DurationSeconds < 600)),
            new DurationBucket(">10m",   sessions.Count(s => s.DurationSeconds >= 600))
        };
    }

    public sealed record DurationBucket(string Label, int Count);
}
