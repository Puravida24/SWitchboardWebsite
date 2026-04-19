using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Computes the nightly <see cref="EventRollupDaily"/> aggregates for a given day.
/// Idempotent — re-running for the same date upserts existing rows rather than
/// duplicating. Called by <see cref="RollupBackgroundService"/> at 02:00 UTC.
/// </summary>
public interface IRollupRunner
{
    Task RollupDayAsync(DateTime day);
    DateTime? LastRunAt { get; }
}

public class RollupRunner : IRollupRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RollupRunner> _logger;

    public DateTime? LastRunAt { get; private set; }

    public RollupRunner(IServiceScopeFactory scopeFactory, ILogger<RollupRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RollupDayAsync(DateTime day)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var start = day.Date;
        var end = start.AddDays(1);

        // Build all rollup rows in memory then upsert.
        var rows = new Dictionary<(string path, string metric, string dim), long>();

        void Bump(string path, string metric, string dim)
        {
            var key = (path, metric, dim);
            rows[key] = rows.GetValueOrDefault(key, 0) + 1;
        }

        var pvs = await db.PageViews.Where(p => p.Timestamp >= start && p.Timestamp < end)
            .Select(p => new { p.Path, p.UtmSource }).ToListAsync();
        foreach (var pv in pvs)
        {
            Bump(pv.Path, "pageviews", "");
            if (!string.IsNullOrEmpty(pv.UtmSource)) Bump(pv.Path, "pageviews", pv.UtmSource);
        }

        var sessions = await db.Sessions.Where(s => s.StartedAt >= start && s.StartedAt < end && !s.IsBot)
            .Select(s => new { s.LandingPath, s.UtmSource, s.DeviceType, s.Converted }).ToListAsync();
        foreach (var s in sessions)
        {
            var p = s.LandingPath ?? "/";
            Bump(p, "sessions", "");
            if (!string.IsNullOrEmpty(s.UtmSource)) Bump(p, "sessions", s.UtmSource);
            if (!string.IsNullOrEmpty(s.DeviceType)) Bump(p, "sessions", s.DeviceType);
            if (s.Converted) Bump(p, "conversions", "");
        }

        var clicks = await db.ClickEvents.Where(c => c.Ts >= start && c.Ts < end)
            .Select(c => new { c.Path, c.IsRage, c.IsDead }).ToListAsync();
        foreach (var c in clicks)
        {
            Bump(c.Path, "clicks", "");
            if (c.IsRage) Bump(c.Path, "clicks-rage", "");
            if (c.IsDead) Bump(c.Path, "clicks-dead", "");
        }

        var forms = await db.FormInteractions.Where(f => f.OccurredAt >= start && f.OccurredAt < end)
            .Select(f => new { f.Path, f.Event }).ToListAsync();
        foreach (var f in forms)
        {
            Bump(f.Path, "form-events", f.Event);
        }

        var errs = await db.JsErrors.Where(e => e.Ts >= start && e.Ts < end)
            .Select(e => new { e.Path }).ToListAsync();
        foreach (var e in errs) Bump(e.Path, "errors", "");

        // Upsert — clear existing rows for this date first so we don't leave stale
        // rows when re-running (data may have been purged between runs).
        var existing = await db.EventRollupDailies.Where(r => r.Date == start).ToListAsync();
        db.EventRollupDailies.RemoveRange(existing);
        await db.SaveChangesAsync();

        foreach (var kvp in rows)
        {
            db.EventRollupDailies.Add(new EventRollupDaily
            {
                Date = start,
                Path = kvp.Key.path,
                Metric = kvp.Key.metric,
                Dimension = kvp.Key.dim,
                Value = kvp.Value
            });
        }

        try
        {
            await db.SaveChangesAsync();
            LastRunAt = DateTime.UtcNow;
            _logger.LogInformation("Rollup {Date:yyyy-MM-dd}: {Count} rows", start, rows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollup persist failed for {Date}", start);
        }
    }
}

/// <summary>
/// Hosted service — runs the rollup once a day at 02:00 UTC, for "yesterday".
/// </summary>
public class RollupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<RollupBackgroundService> _logger;

    public RollupBackgroundService(IServiceProvider services, ILogger<RollupBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddHours(2);
                if (nextRun <= now) nextRun = nextRun.AddDays(1);
                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);

                using var scope = _services.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IRollupRunner>();
                await runner.RollupDayAsync(DateTime.UtcNow.Date.AddDays(-1));
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rollup background loop tick failed");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
