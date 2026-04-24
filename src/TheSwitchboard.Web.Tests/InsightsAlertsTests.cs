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

    // ── T12_11 Diagnostic: surface the actual exception type InMemory throws on dup-PK ──
    // Direct test of EF 9 InMemory behavior. If this throws bare ArgumentException,
    // the SessionService catch on DbUpdateException is insufficient and must broaden.
    [Fact]
    public async Task T12_11_Diagnostic_InMemory_DuplicateKey_ExceptionType()
    {
        using var s1 = _factory.Services.CreateScope();
        using var s2 = _factory.Services.CreateScope();
        var db1 = s1.ServiceProvider.GetRequiredService<AppDbContext>();
        var db2 = s2.ServiceProvider.GetRequiredService<AppDbContext>();

        db1.Visitors.RemoveRange(db1.Visitors.Where(v => v.Id == "diag-dup-key"));
        await db1.SaveChangesAsync();

        var vid = "diag-dup-key";
        db1.Visitors.Add(new Visitor { Id = vid, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        await db1.SaveChangesAsync();

        db2.Visitors.Add(new Visitor { Id = vid, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        var thrown = await Record.ExceptionAsync(() => db2.SaveChangesAsync());
        Assert.NotNull(thrown);
        // Log the actual type so we can see it in test output; assert SessionService's
        // current catch would handle it. If neither DbUpdateException nor
        // ArgumentException, the catch needs broader handling.
        var t = thrown!.GetType().FullName ?? "";
        Assert.True(
            thrown is DbUpdateException || thrown is ArgumentException,
            $"InMemory dup-key threw {t}: {thrown.Message} (inner: {thrown.InnerException?.GetType().FullName ?? "none"}). SessionService catch must handle this exact type.");
    }

    // ── T12_12 Heavy-concurrency stress — 50 parallel upserts, no exceptions ──
    // The try/catch-with-retry pattern in UpsertAsync has a narrower residual race:
    // thread C retries, queries, sees existing, updates. Thread D does the same.
    // Both call SaveChanges. Because UPDATE (not INSERT) doesn't collide, this is
    // safe — but other paths in UpsertAsync (Session create + Visitor create in
    // the same transaction) can still collide on the retry path. Run 50 parallel
    // calls to deterministically flush every race window; assert clean exit +
    // exactly one row each.
    [Fact]
    public async Task T12_12_Upsert_50Parallel_SameSidVid_NoException_ExactlyOneRow()
    {
        var sid = "s_heavy_" + Guid.NewGuid().ToString("N")[..8];
        var vid = "v_heavy_" + Guid.NewGuid().ToString("N")[..8];
        var input = new TheSwitchboard.Web.Services.Tracking.UpsertInput(
            Vid: vid, Sid: sid, Path: "/", UserAgent: "test", IpAddress: null,
            Referrer: null, UtmSource: null, UtmMedium: null, UtmCampaign: null,
            UtmTerm: null, UtmContent: null, Gclid: null, Fbclid: null, Msclkid: null,
            ViewportW: null, ViewportH: null, ConsentState: null,
            EventKind: TheSwitchboard.Web.Services.Tracking.EventKind.Pageview, IpHash: null);

        var tasks = new List<Task>();
        var scopes = new List<IServiceScope>();
        try
        {
            for (var i = 0; i < 50; i++)
            {
                var scope = _factory.Services.CreateScope();
                scopes.Add(scope);
                var svc = scope.ServiceProvider.GetRequiredService<TheSwitchboard.Web.Services.Tracking.ISessionService>();
                tasks.Add(Task.Run(() => svc.UpsertAsync(input)));
            }

            var ex = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
            Assert.Null(ex);
        }
        finally
        {
            foreach (var s in scopes) s.Dispose();
        }

        using var verify = _factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Sessions.CountAsync(x => x.Id == sid));
        Assert.Equal(1, await db.Visitors.CountAsync(x => x.Id == vid));
    }

    // ── T12_10 UpsertAsync must catch ArgumentException from InMemory Dictionary.Add ──
    // EF Core 9's InMemory provider does NOT wrap Dictionary.Add's ArgumentException
    // in DbUpdateException — it leaks through SaveChangesAsync. The original catch
    // only handled DbUpdateException, so on a duplicate-key race the ArgumentException
    // propagated all the way to the middleware and produced a 500. Stress-test with
    // many parallel calls to deterministically surface the race and verify no
    // ArgumentException escapes.
    [Fact]
    public async Task T12_10_Upsert_SurvivesManyParallelCalls_NoArgumentExceptionLeak()
    {
        var sid = "s_stress_" + Guid.NewGuid().ToString("N")[..8];
        var vid = "v_stress_" + Guid.NewGuid().ToString("N")[..8];
        var input = new TheSwitchboard.Web.Services.Tracking.UpsertInput(
            Vid: vid, Sid: sid, Path: "/", UserAgent: "test", IpAddress: null,
            Referrer: null, UtmSource: null, UtmMedium: null, UtmCampaign: null,
            UtmTerm: null, UtmContent: null, Gclid: null, Fbclid: null, Msclkid: null,
            ViewportW: null, ViewportH: null, ConsentState: null,
            EventKind: TheSwitchboard.Web.Services.Tracking.EventKind.Pageview, IpHash: null);

        // Spawn 20 parallel upserts across 20 scopes — with a shared InMemory store
        // this reliably collides on the first add.
        var tasks = new List<Task>();
        var scopes = new List<IServiceScope>();
        try
        {
            for (var i = 0; i < 20; i++)
            {
                var scope = _factory.Services.CreateScope();
                scopes.Add(scope);
                var svc = scope.ServiceProvider.GetRequiredService<TheSwitchboard.Web.Services.Tracking.ISessionService>();
                tasks.Add(svc.UpsertAsync(input));
            }

            var ex = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
            Assert.Null(ex);
        }
        finally
        {
            foreach (var s in scopes) s.Dispose();
        }

        using var verify = _factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Sessions.CountAsync(x => x.Id == sid));
        Assert.Equal(1, await db.Visitors.CountAsync(x => x.Id == vid));
    }

    // ── T12_09 SessionService.UpsertAsync survives concurrent same-sid/vid ──
    // PlaywrightSecurityTests.A2_03 flaked intermittently with an InMemory
    // "An item with the same key has already been added" error at the Session
    // + Visitor upsert path. Two near-simultaneous requests both ran the
    // "query → null → Add" branch, then both called SaveChanges. The original
    // retry block replayed only the Session bump, leaving the Visitor add to
    // re-throw. Regression test: fire two parallel UpsertAsync calls against
    // separate scopes with identical sid/vid; assert neither throws and the
    // row exists exactly once.
    [Fact]
    public async Task T12_09_Session_Upsert_ConcurrentCallsSameSidVid_NoException()
    {
        var sid = "s_race_" + Guid.NewGuid().ToString("N")[..8];
        var vid = "v_race_" + Guid.NewGuid().ToString("N")[..8];
        var input = new TheSwitchboard.Web.Services.Tracking.UpsertInput(
            Vid: vid, Sid: sid, Path: "/", UserAgent: "test", IpAddress: null,
            Referrer: null, UtmSource: null, UtmMedium: null, UtmCampaign: null,
            UtmTerm: null, UtmContent: null, Gclid: null, Fbclid: null, Msclkid: null,
            ViewportW: null, ViewportH: null, ConsentState: null,
            EventKind: TheSwitchboard.Web.Services.Tracking.EventKind.Pageview, IpHash: null);

        using var s1 = _factory.Services.CreateScope();
        using var s2 = _factory.Services.CreateScope();
        var svc1 = s1.ServiceProvider.GetRequiredService<TheSwitchboard.Web.Services.Tracking.ISessionService>();
        var svc2 = s2.ServiceProvider.GetRequiredService<TheSwitchboard.Web.Services.Tracking.ISessionService>();

        // Parallel calls — whichever saves first wins the INSERT; the other must
        // catch the DbUpdateException and retry through the reload-both-and-bump
        // path. Neither call should propagate an exception.
        var ex = await Record.ExceptionAsync(() => Task.WhenAll(svc1.UpsertAsync(input), svc2.UpsertAsync(input)));
        Assert.Null(ex);

        using var verify = _factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Sessions.CountAsync(x => x.Id == sid));
        Assert.Equal(1, await db.Visitors.CountAsync(x => x.Id == vid));
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

    // ── T12_08 Capture-rate alert must NOT fire on tiny sample ─────────
    // One form submission without a consent cert = 0% capture rate =
    // trips `capture-rate-below-95` with noise. Same shape as bot-rate.
    [Fact]
    public async Task T12_08_CaptureRateAlert_Suppressed_WhenSampleBelowMinimum()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.FormSubmissions.RemoveRange(db.FormSubmissions);
        db.AlertLogs.RemoveRange(db.AlertLogs);
        await db.SaveChangesAsync();

        // Seed one submission WITHOUT a cert — 0% capture rate in the window.
        db.FormSubmissions.Add(new TheSwitchboard.Web.Models.Forms.FormSubmission
        {
            FormType = "contact",
            Data = "{}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ConsentCertificateId = null
        });
        await db.SaveChangesAsync();

        var rule = await db.AlertRules.FirstOrDefaultAsync(r => r.Name == "capture-rate-below-95");
        if (rule is null)
        {
            rule = new AlertRule
            {
                Name = "capture-rate-below-95",
                MetricExpression = "capture-rate-1h",
                Comparison = "lt",
                Threshold = 95,
                Window = "1h",
                Enabled = true
            };
            db.AlertRules.Add(rule);
            await db.SaveChangesAsync();
        }

        var svc = scope.ServiceProvider.GetRequiredService<IAlertEvaluatorService>();
        await svc.EvaluateAsync();

        var fired = await db.AlertLogs.AnyAsync(l => l.RuleId == rule.Id);
        Assert.False(fired, "capture-rate-below-95 must NOT fire when total submission sample is below the minimum.");
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
