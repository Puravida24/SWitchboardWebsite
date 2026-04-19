using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Drives /Admin/Reports/Frustration and /Admin/Reports/Heatmaps/Click. Queries
/// ClickEvent rows and rolls them up by selector / path for admin surfaces.
/// </summary>
public interface IFrustrationAnalyticsService
{
    Task<IReadOnlyList<FrustrationRow>> RageClicksAsync(DateTime fromUtc, DateTime toUtc, int limit = 50);
    Task<IReadOnlyList<FrustrationRow>> DeadClicksAsync(DateTime fromUtc, DateTime toUtc, int limit = 50);
    Task<IReadOnlyList<FrustratedPage>> TopFrustratedPagesAsync(DateTime fromUtc, DateTime toUtc, int limit = 20);
    Task<IReadOnlyList<ClickDot>> ClickDotsForPathAsync(string path, DateTime fromUtc, DateTime toUtc, int limit = 3000);
    Task<FrustrationSummary> SummaryAsync(DateTime fromUtc, DateTime toUtc);
    Task<IReadOnlyList<string>> DistinctPathsAsync(DateTime fromUtc, DateTime toUtc, int limit = 50);
}

public sealed record FrustrationRow(string Selector, string? SampleText, string Path, int Count);
public sealed record FrustratedPage(string Path, int Rage, int Dead, int Total);
public sealed record ClickDot(int X, int Y, int ViewportW, int ViewportH, bool IsRage, bool IsDead);
public sealed record FrustrationSummary(int TotalClicks, int RageClicks, int DeadClicks, int UniquePaths);

public class FrustrationAnalyticsService : IFrustrationAnalyticsService
{
    private readonly AppDbContext _db;
    public FrustrationAnalyticsService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<FrustrationRow>> RageClicksAsync(DateTime fromUtc, DateTime toUtc, int limit = 50)
    {
        var rows = await _db.ClickEvents
            .Where(c => c.IsRage && c.Ts >= fromUtc && c.Ts <= toUtc)
            .Select(c => new { c.Selector, c.ElementText, c.Path })
            .ToListAsync();
        return rows
            .GroupBy(r => (r.Selector, r.Path))
            .Select(g => new FrustrationRow(g.Key.Selector, g.First().ElementText, g.Key.Path, g.Count()))
            .OrderByDescending(r => r.Count)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<FrustrationRow>> DeadClicksAsync(DateTime fromUtc, DateTime toUtc, int limit = 50)
    {
        var rows = await _db.ClickEvents
            .Where(c => c.IsDead && c.Ts >= fromUtc && c.Ts <= toUtc)
            .Select(c => new { c.Selector, c.ElementText, c.Path })
            .ToListAsync();
        return rows
            .GroupBy(r => (r.Selector, r.Path))
            .Select(g => new FrustrationRow(g.Key.Selector, g.First().ElementText, g.Key.Path, g.Count()))
            .OrderByDescending(r => r.Count)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<FrustratedPage>> TopFrustratedPagesAsync(DateTime fromUtc, DateTime toUtc, int limit = 20)
    {
        var rows = await _db.ClickEvents
            .Where(c => c.Ts >= fromUtc && c.Ts <= toUtc)
            .Select(c => new { c.Path, c.IsRage, c.IsDead })
            .ToListAsync();
        return rows
            .GroupBy(r => r.Path)
            .Select(g => new FrustratedPage(
                Path: g.Key,
                Rage: g.Count(x => x.IsRage),
                Dead: g.Count(x => x.IsDead),
                Total: g.Count()))
            .Where(p => p.Rage > 0 || p.Dead > 0)
            .OrderByDescending(p => p.Rage + p.Dead)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<ClickDot>> ClickDotsForPathAsync(string path, DateTime fromUtc, DateTime toUtc, int limit = 3000)
    {
        return await _db.ClickEvents
            .Where(c => c.Path == path && c.Ts >= fromUtc && c.Ts <= toUtc && c.ViewportW > 0 && c.ViewportH > 0)
            .OrderByDescending(c => c.Ts)
            .Take(limit)
            .Select(c => new ClickDot(c.X, c.Y, c.ViewportW, c.ViewportH, c.IsRage, c.IsDead))
            .ToListAsync();
    }

    public async Task<FrustrationSummary> SummaryAsync(DateTime fromUtc, DateTime toUtc)
    {
        var total = await _db.ClickEvents.CountAsync(c => c.Ts >= fromUtc && c.Ts <= toUtc);
        var rage = await _db.ClickEvents.CountAsync(c => c.IsRage && c.Ts >= fromUtc && c.Ts <= toUtc);
        var dead = await _db.ClickEvents.CountAsync(c => c.IsDead && c.Ts >= fromUtc && c.Ts <= toUtc);
        var paths = await _db.ClickEvents
            .Where(c => c.Ts >= fromUtc && c.Ts <= toUtc)
            .Select(c => c.Path)
            .Distinct()
            .CountAsync();
        return new FrustrationSummary(total, rage, dead, paths);
    }

    public async Task<IReadOnlyList<string>> DistinctPathsAsync(DateTime fromUtc, DateTime toUtc, int limit = 50)
    {
        return await _db.ClickEvents
            .Where(c => c.Ts >= fromUtc && c.Ts <= toUtc)
            .Select(c => c.Path)
            .Distinct()
            .OrderBy(p => p)
            .Take(limit)
            .ToListAsync();
    }
}
