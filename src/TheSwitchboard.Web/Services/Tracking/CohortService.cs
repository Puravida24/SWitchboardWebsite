using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// /Admin/Reports/Cohorts — weekly retention grid. Visitors are bucketed by
/// the week of their FirstSeen; retention for week-N = % of that cohort who
/// had a session in calendar week N+N.
/// </summary>
public interface ICohortService
{
    Task<IReadOnlyList<CohortRow>> WeeklyCohortsAsync(DateTime fromUtc, int weeksCount = 8);
}

public sealed record CohortRow(DateTime CohortStart, int Size, IReadOnlyList<int> WeekRetentionPct);

public class CohortService : ICohortService
{
    private readonly AppDbContext _db;
    public CohortService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<CohortRow>> WeeklyCohortsAsync(DateTime fromUtc, int weeksCount = 8)
    {
        var start = StartOfWeek(fromUtc.Date);
        var end = start.AddDays(weeksCount * 7);

        var visitors = await _db.Visitors
            .Where(v => v.FirstSeen >= start && v.FirstSeen < end)
            .Select(v => new { v.Id, v.FirstSeen })
            .ToListAsync();
        var sessions = await _db.Sessions
            .Where(s => s.StartedAt >= start && s.StartedAt < end && !s.IsBot && s.VisitorId != null)
            .Select(s => new { s.VisitorId, s.StartedAt })
            .ToListAsync();

        var sessionsByVid = sessions
            .GroupBy(s => s.VisitorId!)
            .ToDictionary(g => g.Key, g => g.Select(s => StartOfWeek(s.StartedAt.Date)).ToHashSet());

        var rows = new List<CohortRow>();
        for (var w = 0; w < weeksCount; w++)
        {
            var cohortStart = start.AddDays(w * 7);
            var cohortEnd = cohortStart.AddDays(7);
            var cohort = visitors
                .Where(v => v.FirstSeen >= cohortStart && v.FirstSeen < cohortEnd)
                .Select(v => v.Id)
                .ToList();
            if (cohort.Count == 0)
            {
                rows.Add(new CohortRow(cohortStart, 0, new List<int>()));
                continue;
            }

            var retention = new List<int>();
            for (var offset = 0; offset < weeksCount - w; offset++)
            {
                var targetWeek = cohortStart.AddDays(offset * 7);
                var returned = cohort.Count(id =>
                    sessionsByVid.TryGetValue(id, out var weeks) && weeks.Contains(targetWeek));
                retention.Add((int)Math.Round(100.0 * returned / cohort.Count));
            }
            rows.Add(new CohortRow(cohortStart, cohort.Count, retention));
        }
        return rows;
    }

    private static DateTime StartOfWeek(DateTime d)
    {
        // Sunday as week start to match US analytics convention.
        var diff = (7 + (int)d.DayOfWeek - (int)DayOfWeek.Sunday) % 7;
        return d.AddDays(-diff).Date;
    }
}
