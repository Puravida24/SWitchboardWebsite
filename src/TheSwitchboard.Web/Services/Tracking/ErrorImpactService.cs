using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Correlates JsError fingerprints with Session.Converted to rank errors by
/// their impact on conversion rate. Drives /Admin/Reports/ErrorImpact.
///
/// CVR-Delta = CVR(sessions affected) − CVR(sessions unaffected). Negative
/// values indicate the error hurts conversion — higher-priority bugs.
/// </summary>
public interface IErrorImpactService
{
    Task<IReadOnlyList<ErrorImpactRow>> ComputeAsync(DateTime fromUtc, DateTime toUtc);
    Task<IReadOnlyList<ErrorSummaryRow>> ErrorListAsync(DateTime fromUtc, DateTime toUtc, int limit = 100);
    Task<PerformanceReport> PerformanceAsync(DateTime fromUtc, DateTime toUtc);
    Task<IReadOnlyList<string>> DistinctPathsAsync(DateTime fromUtc, DateTime toUtc);
}

public sealed record ErrorImpactRow(
    string Fingerprint,
    string Message,
    int SessionsAffected,
    int SessionsUnaffected,
    int CvrAffectedPct,
    int CvrUnaffectedPct,
    int CvrDeltaPct,
    DateTime? LastSeenAt);

public sealed record ErrorSummaryRow(
    string Fingerprint,
    string Message,
    string? Source,
    int? Line,
    int Occurrences,
    int DistinctSessions,
    DateTime? LastSeenAt);

public sealed record WebVitalRow(string Path, string Metric, double P75, int Good, int NeedsImprovement, int Poor, string Rating);
public sealed record PerformanceReport(IReadOnlyList<WebVitalRow> Rows);

public class ErrorImpactService : IErrorImpactService
{
    private readonly AppDbContext _db;
    public ErrorImpactService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<ErrorImpactRow>> ComputeAsync(DateTime fromUtc, DateTime toUtc)
    {
        var errors = await _db.JsErrors
            .Where(e => e.Ts >= fromUtc && e.Ts <= toUtc && e.Fingerprint != null)
            .Select(e => new { e.Fingerprint, e.SessionId, e.Message, e.LastSeenAt })
            .ToListAsync();

        var sessionConverted = await _db.Sessions
            .Where(s => s.StartedAt >= fromUtc.AddDays(-1) && s.StartedAt <= toUtc)
            .Select(s => new { s.Id, s.Converted, s.IsBot })
            .ToListAsync();
        var convertedMap = sessionConverted.ToDictionary(s => s.Id, s => s);
        var totalSessions = sessionConverted.Count(s => !s.IsBot);
        var totalConverted = sessionConverted.Count(s => !s.IsBot && s.Converted);

        var results = new List<ErrorImpactRow>();
        foreach (var group in errors.GroupBy(e => e.Fingerprint!))
        {
            var affectedSids = group.Select(e => e.SessionId).Distinct().ToHashSet();
            var affectedSessions = affectedSids.Where(convertedMap.ContainsKey)
                .Select(id => convertedMap[id])
                .Where(s => !s.IsBot)
                .ToList();
            if (affectedSessions.Count == 0) continue;

            var affectedConverted = affectedSessions.Count(s => s.Converted);
            var cvrAffected = (int)Math.Round(100.0 * affectedConverted / Math.Max(1, affectedSessions.Count));

            var unaffectedTotal = Math.Max(0, totalSessions - affectedSessions.Count);
            var unaffectedConverted = Math.Max(0, totalConverted - affectedConverted);
            var cvrUnaffected = unaffectedTotal > 0
                ? (int)Math.Round(100.0 * unaffectedConverted / unaffectedTotal)
                : 0;

            results.Add(new ErrorImpactRow(
                Fingerprint: group.Key,
                Message: group.First().Message,
                SessionsAffected: affectedSessions.Count,
                SessionsUnaffected: unaffectedTotal,
                CvrAffectedPct: cvrAffected,
                CvrUnaffectedPct: cvrUnaffected,
                CvrDeltaPct: cvrAffected - cvrUnaffected,
                LastSeenAt: group.Max(g => g.LastSeenAt)));
        }
        return results.OrderBy(r => r.CvrDeltaPct).ToList();
    }

    public async Task<IReadOnlyList<ErrorSummaryRow>> ErrorListAsync(DateTime fromUtc, DateTime toUtc, int limit = 100)
    {
        var rows = await _db.JsErrors
            .Where(e => e.Ts >= fromUtc && e.Ts <= toUtc)
            .Select(e => new { e.Fingerprint, e.Message, e.Source, e.Line, e.Count, e.SessionId, e.LastSeenAt })
            .ToListAsync();

        return rows
            .Where(r => r.Fingerprint is not null)
            .GroupBy(r => r.Fingerprint!)
            .Select(g => new ErrorSummaryRow(
                g.Key,
                g.First().Message,
                g.First().Source,
                g.First().Line,
                g.Sum(x => x.Count),
                g.Select(x => x.SessionId).Distinct().Count(),
                g.Max(x => x.LastSeenAt)))
            .OrderByDescending(r => r.Occurrences)
            .Take(limit)
            .ToList();
    }

    public async Task<PerformanceReport> PerformanceAsync(DateTime fromUtc, DateTime toUtc)
    {
        var rows = await _db.WebVitalSamples
            .Where(v => v.Ts >= fromUtc && v.Ts <= toUtc)
            .Select(v => new { v.Path, v.Metric, v.Value, v.Rating })
            .ToListAsync();

        var output = rows
            .GroupBy(r => (r.Path, r.Metric))
            .Select(g =>
            {
                var values = g.Select(x => x.Value).OrderBy(v => v).ToList();
                var p75 = values.Count == 0 ? 0 : values[(int)Math.Ceiling(values.Count * 0.75) - 1];
                var good = g.Count(x => x.Rating == "good");
                var ni = g.Count(x => x.Rating == "ni");
                var poor = g.Count(x => x.Rating == "poor");
                var bucket = poor > 0 && poor >= g.Count() / 4 ? "poor"
                           : ni   > 0 && ni   >= g.Count() / 2 ? "ni"
                           : "good";
                return new WebVitalRow(g.Key.Path, g.Key.Metric, p75, good, ni, poor, bucket);
            })
            .OrderBy(r => r.Path).ThenBy(r => r.Metric)
            .ToList();
        return new PerformanceReport(output);
    }

    public async Task<IReadOnlyList<string>> DistinctPathsAsync(DateTime fromUtc, DateTime toUtc)
    {
        var a = await _db.WebVitalSamples.Where(v => v.Ts >= fromUtc && v.Ts <= toUtc).Select(v => v.Path).Distinct().ToListAsync();
        var b = await _db.JsErrors.Where(e => e.Ts >= fromUtc && e.Ts <= toUtc).Select(e => e.Path).Distinct().ToListAsync();
        return a.Union(b).OrderBy(p => p).ToList();
    }
}
