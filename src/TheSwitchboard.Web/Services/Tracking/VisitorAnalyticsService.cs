using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// /Admin/Reports/Visitors — unique visitor list + per-visitor profile.
/// </summary>
public interface IVisitorAnalyticsService
{
    Task<IReadOnlyList<VisitorRow>> ListAsync(DateTime fromUtc, DateTime toUtc, int skip = 0, int take = 100);
    Task<int> CountAsync(DateTime fromUtc, DateTime toUtc);
    Task<VisitorProfile?> GetProfileAsync(string visitorId);
}

public sealed record VisitorRow(string Id, DateTime FirstSeen, DateTime LastSeen, int SessionCount, bool Converted);
public sealed record VisitorProfile(
    Visitor Visitor,
    IReadOnlyList<Session> Sessions,
    int PageViewCount,
    int SubmissionCount,
    int ErrorCount);

public class VisitorAnalyticsService : IVisitorAnalyticsService
{
    private readonly AppDbContext _db;
    public VisitorAnalyticsService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<VisitorRow>> ListAsync(DateTime fromUtc, DateTime toUtc, int skip = 0, int take = 100)
    {
        return await _db.Visitors
            .Where(v => v.LastSeen >= fromUtc && v.FirstSeen <= toUtc)
            .OrderByDescending(v => v.LastSeen)
            .Skip(skip)
            .Take(take)
            .Select(v => new VisitorRow(v.Id, v.FirstSeen, v.LastSeen, v.SessionCount, v.ConvertedAt.HasValue))
            .ToListAsync();
    }

    public async Task<int> CountAsync(DateTime fromUtc, DateTime toUtc)
    {
        return await _db.Visitors.CountAsync(v => v.LastSeen >= fromUtc && v.FirstSeen <= toUtc);
    }

    public async Task<VisitorProfile?> GetProfileAsync(string visitorId)
    {
        var visitor = await _db.Visitors.FirstOrDefaultAsync(v => v.Id == visitorId);
        if (visitor is null) return null;

        var sessions = await _db.Sessions
            .Where(s => s.VisitorId == visitorId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
        var sessionIds = sessions.Select(s => s.Id).ToList();
        var pv = await _db.PageViews.CountAsync(p => sessionIds.Contains(p.SessionId!));
        var errs = await _db.JsErrors.CountAsync(e => sessionIds.Contains(e.SessionId));
        var subs = await _db.FormSubmissions.CountAsync(s =>
            s.ConsentCertificateId != null &&
            _db.ConsentCertificates.Any(c => c.Id == s.ConsentCertificateId && sessionIds.Contains(c.SessionId!)));

        return new VisitorProfile(visitor, sessions, pv, subs, errs);
    }
}
