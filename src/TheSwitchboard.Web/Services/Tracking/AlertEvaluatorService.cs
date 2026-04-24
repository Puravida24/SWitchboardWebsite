using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Periodic alert rule evaluator. Each rule names a metric expression + window
/// (e.g. "js-errors-1h") and a comparison + threshold. Firing rules append to
/// AlertLog; email/webhook delivery is decoupled (delivered by dispatcher).
/// </summary>
public interface IAlertEvaluatorService
{
    Task EvaluateAsync(DateTime? nowUtc = null);
}

public class AlertEvaluatorService : IAlertEvaluatorService
{
    private readonly AppDbContext _db;
    private readonly IAlertDispatcher _dispatcher;
    private readonly ILogger<AlertEvaluatorService> _logger;
    public AlertEvaluatorService(AppDbContext db, IAlertDispatcher dispatcher, ILogger<AlertEvaluatorService> logger)
    {
        _db = db; _dispatcher = dispatcher; _logger = logger;
    }

    public async Task EvaluateAsync(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var rules = await _db.AlertRules.Where(r => r.Enabled).ToListAsync();
        foreach (var r in rules)
        {
            var windowFrom = r.Window switch
            {
                "5m"  => now.AddMinutes(-5),
                "1h"  => now.AddHours(-1),
                "24h" => now.AddHours(-24),
                _     => now.AddHours(-1)
            };

            double value = r.MetricExpression switch
            {
                "js-errors-1h"     => await _db.JsErrors.CountAsync(e => e.Ts >= windowFrom && e.Ts <= now),
                "js-errors-5m"     => await _db.JsErrors.CountAsync(e => e.Ts >= windowFrom && e.Ts <= now),
                "bot-rate-1h"      => await BotRate(windowFrom, now),
                "capture-rate-1h"  => await CaptureRate(windowFrom, now),
                _                  => 0
            };

            var fired = r.Comparison switch
            {
                "gt" => value > r.Threshold,
                "lt" => value < r.Threshold,
                "eq" => Math.Abs(value - r.Threshold) < 0.001,
                _    => false
            };
            if (!fired) continue;

            // Dedupe: don't re-fire within the rule's window if we already logged.
            var alreadyFired = await _db.AlertLogs
                .AnyAsync(l => l.RuleId == r.Id && l.FiredAt >= windowFrom);
            if (alreadyFired) continue;

            var log = new AlertLog
            {
                RuleId = r.Id,
                FiredAt = now,
                Value = value,
                Threshold = r.Threshold,
                Severity = value > r.Threshold * 2 ? "critical" : "warn",
                Context = $"{r.MetricExpression} = {value} {r.Comparison} {r.Threshold}"
            };
            _db.AlertLogs.Add(log);
            _logger.LogWarning("Alert {Rule} fired: {Value} {Op} {Threshold}", r.Name, value, r.Comparison, r.Threshold);
            // Save before dispatch so the row exists even if dispatch fails.
            await _db.SaveChangesAsync();
            await _dispatcher.DispatchAsync(r, log);
        }
        await _db.SaveChangesAsync();
    }

    // Minimum session sample before bot-rate is a meaningful percentage. Without
    // this guard a single bot-classified session in a quiet hour produces 100%
    // and trips the 5% threshold, spamming CRITICAL alerts. 10 sessions is a
    // pragmatic floor — under it, one crawler hit stays under threshold.
    private const int BotRateMinSampleSize = 10;

    private async Task<double> BotRate(DateTime from, DateTime to)
    {
        var total = await _db.Sessions.CountAsync(s => s.StartedAt >= from && s.StartedAt <= to);
        if (total < BotRateMinSampleSize) return 0;
        var bots = await _db.Sessions.CountAsync(s => s.StartedAt >= from && s.StartedAt <= to && s.IsBot);
        return 100.0 * bots / total;
    }

    // Same min-sample rationale as BotRate — a single submission without a cert
    // produces 0% and trips the 95% floor. Below this floor the rate is meaningless
    // so return 100 (pass-through) to suppress the rule.
    private const int CaptureRateMinSampleSize = 10;

    private async Task<double> CaptureRate(DateTime from, DateTime to)
    {
        var subs = await _db.FormSubmissions.CountAsync(s => s.CreatedAt >= from && s.CreatedAt <= to);
        if (subs < CaptureRateMinSampleSize) return 100;
        var certified = await _db.FormSubmissions.CountAsync(s => s.CreatedAt >= from && s.CreatedAt <= to && s.ConsentCertificateId != null);
        return 100.0 * certified / subs;
    }
}
