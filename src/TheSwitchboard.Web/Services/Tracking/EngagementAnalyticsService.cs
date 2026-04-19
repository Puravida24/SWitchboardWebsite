using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Drives /Admin/Reports/Forms/Funnel, /Admin/Reports/Abandonment, /Admin/Reports/Heatmaps/Scroll.
/// Groups FormInteraction / ScrollSample / Session rows to expose per-field drop-off, exit paths,
/// abandoned fields, and scroll depth bands.
/// </summary>
public interface IEngagementAnalyticsService
{
    Task<FunnelReport> FunnelAsync(string formId, DateTime fromUtc, DateTime toUtc);
    Task<AbandonmentReport> AbandonmentAsync(DateTime fromUtc, DateTime toUtc);
    Task<ScrollHeatmap> ScrollAsync(string path, DateTime fromUtc, DateTime toUtc);
    Task<IReadOnlyList<string>> DistinctFormsAsync(DateTime fromUtc, DateTime toUtc);
    Task<IReadOnlyList<string>> DistinctScrollPathsAsync(DateTime fromUtc, DateTime toUtc);
}

public sealed record FunnelStep(string FieldName, int Reached, int Completed, int Errored, int PastedCount, int AvgDwellMs, int DropOffPct);
public sealed record FunnelReport(string FormId, int Starts, int Submits, int Abandons, IReadOnlyList<FunnelStep> Steps);

public sealed record AbandonmentRow(string FieldName, int Count, int AvgDwellMs);
public sealed record ExitPathRow(string Path, int Exits);
public sealed record AbandonmentReport(int TotalSessions, int AbandonedSessions, IReadOnlyList<AbandonmentRow> AbandonedFields, IReadOnlyList<ExitPathRow> TopExitPaths);

public sealed record ScrollBand(int Depth, int Reached, double Share);
public sealed record ScrollHeatmap(string Path, int UniqueSessions, IReadOnlyList<ScrollBand> Bands);

public class EngagementAnalyticsService : IEngagementAnalyticsService
{
    private readonly AppDbContext _db;
    public EngagementAnalyticsService(AppDbContext db) { _db = db; }

    public async Task<FunnelReport> FunnelAsync(string formId, DateTime fromUtc, DateTime toUtc)
    {
        // Gather every event for this form in-window.
        var events = await _db.FormInteractions
            .Where(f => f.FormId == formId && f.OccurredAt >= fromUtc && f.OccurredAt <= toUtc)
            .Select(f => new { f.SessionId, f.FieldName, f.Event, f.DwellMs, f.PastedFlag })
            .ToListAsync();

        if (events.Count == 0) return new FunnelReport(formId, 0, 0, 0, Array.Empty<FunnelStep>());

        // Canonical step order for the contact form; fall back to discovered field names.
        var canonical = new[] { "name", "email", "company", "role", "phone", "message" };
        var discovered = events.Select(e => e.FieldName)
            .Where(n => n != "(form)")
            .Distinct()
            .ToList();
        var order = canonical.Where(discovered.Contains)
            .Concat(discovered.Where(n => !canonical.Contains(n)))
            .ToList();

        var starts  = events.Count(e => e.FieldName == canonical[0] && string.Equals(e.Event, "focus", StringComparison.OrdinalIgnoreCase));
        var submits = events.Count(e => string.Equals(e.Event, "submit", StringComparison.OrdinalIgnoreCase));
        var abandons = events.Count(e => string.Equals(e.Event, "abandon", StringComparison.OrdinalIgnoreCase));

        var byField = events
            .Where(e => order.Contains(e.FieldName))
            .GroupBy(e => e.FieldName)
            .ToDictionary(g => g.Key, g => g.ToList());

        var prevReached = 0;
        var steps = new List<FunnelStep>();
        for (var i = 0; i < order.Count; i++)
        {
            var name = order[i];
            var rows = byField.TryGetValue(name, out var lst) ? lst : new();
            var reached   = rows.Select(r => r.SessionId).Distinct().Count();
            var completed = rows.Count(r => string.Equals(r.Event, "blur", StringComparison.OrdinalIgnoreCase));
            var errored   = rows.Count(r => string.Equals(r.Event, "error", StringComparison.OrdinalIgnoreCase));
            var pasted    = rows.Count(r => r.PastedFlag == true);
            var dwell = rows.Where(r => r.DwellMs.HasValue).Select(r => r.DwellMs!.Value).ToArray();
            var avgDwell = dwell.Length > 0 ? (int)dwell.Average() : 0;
            var dropOff = (i > 0 && prevReached > 0)
                ? (int)Math.Round((1.0 - (double)reached / prevReached) * 100)
                : 0;
            steps.Add(new FunnelStep(name, reached, completed, errored, pasted, avgDwell, Math.Max(0, dropOff)));
            prevReached = reached;
        }

        return new FunnelReport(formId, starts, submits, abandons, steps);
    }

    public async Task<AbandonmentReport> AbandonmentAsync(DateTime fromUtc, DateTime toUtc)
    {
        var total = await _db.Sessions.CountAsync(s => s.StartedAt >= fromUtc && s.StartedAt <= toUtc);

        var abandonEvents = await _db.FormInteractions
            .Where(f => f.Event == "abandon" && f.OccurredAt >= fromUtc && f.OccurredAt <= toUtc)
            .Select(f => new { f.SessionId, f.FieldName, f.DwellMs })
            .ToListAsync();

        var fields = abandonEvents
            .GroupBy(e => e.FieldName)
            .Select(g => new AbandonmentRow(
                g.Key,
                g.Select(x => x.SessionId).Distinct().Count(),
                g.Where(x => x.DwellMs.HasValue).Select(x => x.DwellMs!.Value).DefaultIfEmpty(0).Sum() /
                    Math.Max(1, g.Count(x => x.DwellMs.HasValue))))
            .OrderByDescending(r => r.Count)
            .ToList();

        var exits = await _db.Sessions
            .Where(s => s.ExitPath != null && s.StartedAt >= fromUtc && s.StartedAt <= toUtc)
            .Select(s => s.ExitPath!)
            .ToListAsync();
        var topExits = exits
            .GroupBy(p => p)
            .Select(g => new ExitPathRow(g.Key, g.Count()))
            .OrderByDescending(r => r.Exits)
            .Take(20)
            .ToList();

        return new AbandonmentReport(total, abandonEvents.Select(e => e.SessionId).Distinct().Count(), fields, topExits);
    }

    public async Task<ScrollHeatmap> ScrollAsync(string path, DateTime fromUtc, DateTime toUtc)
    {
        var sessions = await _db.ScrollSamples
            .Where(s => s.Path == path && s.Ts >= fromUtc && s.Ts <= toUtc)
            .Select(s => s.SessionId)
            .Distinct()
            .CountAsync();

        var bands = new List<ScrollBand>();
        int[] ladder = { 25, 50, 75, 100 };
        foreach (var d in ladder)
        {
            var reached = await _db.ScrollSamples
                .Where(s => s.Path == path && s.Depth >= d && s.Ts >= fromUtc && s.Ts <= toUtc)
                .Select(s => s.SessionId)
                .Distinct()
                .CountAsync();
            var share = sessions > 0 ? (double)reached / sessions : 0;
            bands.Add(new ScrollBand(d, reached, share));
        }
        return new ScrollHeatmap(path, sessions, bands);
    }

    public async Task<IReadOnlyList<string>> DistinctFormsAsync(DateTime fromUtc, DateTime toUtc)
    {
        return await _db.FormInteractions
            .Where(f => f.OccurredAt >= fromUtc && f.OccurredAt <= toUtc)
            .Select(f => f.FormId)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> DistinctScrollPathsAsync(DateTime fromUtc, DateTime toUtc)
    {
        return await _db.ScrollSamples
            .Where(s => s.Ts >= fromUtc && s.Ts <= toUtc)
            .Select(s => s.Path)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }
}
