using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Analytics;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice T-12 — Insights + Alerts + Segments.
///
///   T12_01 — InsightsService produces at least one Insight row when a day's
///            metric deviates sharply from its 7-day trailing mean.
///   T12_02 — AlertEvaluatorService fires an AlertLog when a rule's metric
///            crosses the configured threshold.
///   T12_03 — SegmentService saves + retrieves a segment filter.
///   T12_04..06 — /Admin/Reports/Insights, /Alerts, /Segments anon redirect.
/// </summary>
public class InsightsAlertsTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InsightsAlertsTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ── T12_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T12_01_InsightsService_DetectsAnomaly()
    {
        // Seed a flat 7-day baseline (~10 PVs/day) then a day-of spike to 80.
        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            for (var d = 1; d <= 7; d++)
                for (var i = 0; i < 10; i++)
                    db.PageViews.Add(new PageView { Path = "/", Timestamp = now.AddDays(-d).AddMinutes(i) });
            for (var i = 0; i < 80; i++)
                db.PageViews.Add(new PageView { Path = "/", Timestamp = now.AddMinutes(-i) });
            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IInsightsService>();
        var insights = await svc.DetectAsync();
        Assert.NotEmpty(insights);
    }

    // ── T12_02 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T12_02_AlertEvaluator_FiresOnThreshold()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.JsErrors.AddRange(Enumerable.Range(0, 60).Select(i =>
            new JsError { SessionId = $"alert_s_{i}", Path = "/", Ts = DateTime.UtcNow.AddMinutes(-i), Message = "boom", Count = 1 }));
        var rule = new AlertRule
        {
            Name = "many-errors",
            MetricExpression = "js-errors-1h",
            Comparison = "gt",
            Threshold = 50,
            Window = "1h",
            Enabled = true
        };
        db.AlertRules.Add(rule);
        await db.SaveChangesAsync();

        var svc = scope.ServiceProvider.GetRequiredService<IAlertEvaluatorService>();
        await svc.EvaluateAsync();

        Assert.True(await db.AlertLogs.AnyAsync(l => l.RuleId == rule.Id));
    }

    // ── T12_07 Bot-rate alert must NOT fire on tiny sample ─────────────
    // A single bot session in a quiet hour produced a 100% bot rate which
    // tripped the 5% threshold and spammed CRITICAL alerts. The rule must
    // require a minimum session sample before evaluating the rate.
    [Fact]
    public async Task T12_07_BotRateAlert_Suppressed_WhenSampleBelowMinimum()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Sessions.RemoveRange(db.Sessions);
        db.AlertLogs.RemoveRange(db.AlertLogs);
        await db.SaveChangesAsync();

        db.Sessions.Add(new Session
        {
            Id = "s_bot_solo_" + Guid.NewGuid().ToString("N")[..8],
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            EndedAt = DateTime.UtcNow.AddMinutes(-5),
            IsBot = true
        });
        await db.SaveChangesAsync();

        var rule = await db.AlertRules.FirstOrDefaultAsync(r => r.Name == "bot-rate-above-5");
        if (rule is null)
        {
            rule = new AlertRule
            {
                Name = "bot-rate-above-5",
                MetricExpression = "bot-rate-1h",
                Comparison = "gt",
                Threshold = 5,
                Window = "1h",
                Enabled = true
            };
            db.AlertRules.Add(rule);
            await db.SaveChangesAsync();
        }

        var svc = scope.ServiceProvider.GetRequiredService<IAlertEvaluatorService>();
        await svc.EvaluateAsync();

        var fired = await db.AlertLogs.AnyAsync(l => l.RuleId == rule.Id);
        Assert.False(fired, "bot-rate-above-5 must NOT fire when total session sample is below the minimum (1 session is not a meaningful rate).");
    }

    // ── T12_03 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T12_03_SegmentService_SavesAndRetrieves()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ISegmentService>();
        var name = "desktop-linkedin-" + Guid.NewGuid().ToString("N")[..6];
        var id = await svc.CreateAsync(name, "{\"device\":\"desktop\",\"source\":\"linkedin\"}", "tester");
        var seg = await svc.GetAsync(id);
        Assert.NotNull(seg);
        Assert.Equal(name, seg!.Name);
        Assert.Contains("linkedin", seg.Filter);
    }

    // ── T12_04..06 ─────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Admin/Reports/Insights")]
    [InlineData("/Admin/Reports/Alerts")]
    [InlineData("/Admin/Reports/Segments")]
    public async Task T12_AdminPages_Anonymous_RedirectsToLogin(string path)
    {
        var res = await _client.GetAsync(path);
        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther);
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
