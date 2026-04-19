using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Enforces data retention policy:
///   90 days — PageView, ClickEvent, ScrollSample, MouseTrail, FormInteraction,
///             WebVitalSample, BrowserSignal, AnalyticsEvent
///   1 year  — JsError (hard delete)
///   1 year  — ReplayChunk.Payload nulled (soft-delete — Replay envelope kept
///             indefinitely for session-count integrity)
/// Never purges: Visitor, Session, Goal, ConsentCertificate (5y via ExpiresAt),
///               DisclosureVersion, KnownProxyAsn, EventRollupDaily.
/// </summary>
public interface IRetentionRunner
{
    Task RunAsync(DateTime? nowUtc = null);
    DateTime? LastRunAt { get; }
}

public class RetentionRunner : IRetentionRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetentionRunner> _logger;
    public DateTime? LastRunAt { get; private set; }

    public RetentionRunner(IServiceScopeFactory scopeFactory, ILogger<RetentionRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunAsync(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var cutoff90 = now.AddDays(-90);
        var cutoff1y = now.AddYears(-1);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 90-day purges — hard delete.
        db.PageViews.RemoveRange(db.PageViews.Where(p => p.Timestamp < cutoff90));
        db.ClickEvents.RemoveRange(db.ClickEvents.Where(c => c.Ts < cutoff90));
        db.ScrollSamples.RemoveRange(db.ScrollSamples.Where(s => s.Ts < cutoff90));
        db.MouseTrails.RemoveRange(db.MouseTrails.Where(m => m.Ts < cutoff90));
        db.FormInteractions.RemoveRange(db.FormInteractions.Where(f => f.OccurredAt < cutoff90));
        db.WebVitalSamples.RemoveRange(db.WebVitalSamples.Where(v => v.Ts < cutoff90));
        db.BrowserSignals.RemoveRange(db.BrowserSignals.Where(b => b.CapturedAt < cutoff90));
        db.AnalyticsEvents.RemoveRange(db.AnalyticsEvents.Where(e => e.Timestamp < cutoff90));

        // 1-year hard delete: JsError.
        db.JsErrors.RemoveRange(db.JsErrors.Where(e => e.Ts < cutoff1y));

        // 1-year soft delete: null out ReplayChunk.Payload but keep Replay envelope.
        var oldChunks = await db.ReplayChunks
            .Where(c => c.Ts < cutoff1y && c.Payload.Length > 0)
            .ToListAsync();
        foreach (var chunk in oldChunks)
        {
            chunk.Payload = Array.Empty<byte>();
        }

        try
        {
            var affected = await db.SaveChangesAsync();
            LastRunAt = DateTime.UtcNow;
            _logger.LogInformation("Retention run: {Affected} rows affected", affected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Retention run failed");
        }
    }
}

public class DataRetentionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DataRetentionBackgroundService> _logger;

    public DataRetentionBackgroundService(IServiceProvider services, ILogger<DataRetentionBackgroundService> logger)
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
                var nextRun = now.Date.AddHours(3);
                if (nextRun <= now) nextRun = nextRun.AddDays(1);
                await Task.Delay(nextRun - now, stoppingToken);

                using var scope = _services.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IRetentionRunner>();
                await runner.RunAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retention background loop tick failed");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
