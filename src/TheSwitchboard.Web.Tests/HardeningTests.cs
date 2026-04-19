using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Hardening slices H-1..H-8. Each test named H-N_NN for traceability.
/// </summary>
public class HardeningTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    public HardeningTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
    }

    // ── H-1_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H1_01_GoalFire_FlipsSessionConverted_AndVisitorConvertedAt()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<IGoalService>();

        var vid = "h1_v_" + Guid.NewGuid().ToString("N")[..8];
        var sid = "h1_s_" + Guid.NewGuid().ToString("N")[..8];
        db.Visitors.Add(new Visitor { Id = vid, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        db.Sessions.Add(new Session { Id = sid, VisitorId = vid, StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow });
        db.Goals.Add(new Goal { Name = "h1-goal", Kind = "form", MatchExpression = "contact", Enabled = true });
        var sub = new TheSwitchboard.Web.Models.Forms.FormSubmission { FormType = "contact", Data = "{}", CreatedAt = DateTime.UtcNow };
        db.FormSubmissions.Add(sub);
        await db.SaveChangesAsync();

        await svc.EvaluateFormSubmissionAsync(sub, sid, vid);

        var session = await db.Sessions.FirstAsync(s => s.Id == sid);
        var visitor = await db.Visitors.FirstAsync(v => v.Id == vid);
        Assert.True(session.Converted, "Session.Converted must flip to true when a goal fires");
        Assert.NotNull(visitor.ConvertedAt);
    }

    // ── H-2_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H2_01_AlertEvaluator_Fires_AndLogs()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.JsErrors.AddRange(Enumerable.Range(0, 30).Select(i =>
            new JsError { SessionId = "h2_s_" + i, Path = "/", Ts = DateTime.UtcNow.AddMinutes(-i), Message = "boom", Count = 1 }));
        var rule = new AlertRule
        {
            Name = "h2-rule-" + Guid.NewGuid().ToString("N")[..6],
            MetricExpression = "js-errors-1h",
            Comparison = "gt", Threshold = 20, Window = "1h", Enabled = true,
            Channel = "email", ChannelTarget = "test@example.com"
        };
        db.AlertRules.Add(rule);
        await db.SaveChangesAsync();

        var evaluator = scope.ServiceProvider.GetRequiredService<IAlertEvaluatorService>();
        await evaluator.EvaluateAsync();

        Assert.True(await db.AlertLogs.AnyAsync(l => l.RuleId == rule.Id));
    }

    // ── H-8_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H8_01_Identify_ForeignOrigin_Returns403()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/identify");
        req.Headers.Add("Origin", "https://evil.example.com");
        req.Content = new StringContent("{\"sid\":\"x\"}", Encoding.UTF8, "application/json");
        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── H-8_02 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H8_02_Identify_StampsEmailHashOnSession()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sid = "h8_s_" + Guid.NewGuid().ToString("N")[..10];
        db.Sessions.Add(new Session { Id = sid, StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/identify");
        req.Headers.Add("Origin", "http://localhost");
        req.Content = new StringContent(
            JsonSerializer.Serialize(new { sid, emailHashHex = "abc123def", role = "carrier" }),
            Encoding.UTF8, "application/json");
        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        using var verify = _factory.Services.CreateScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await db2.Sessions.FirstAsync(s => s.Id == sid);
        Assert.Equal("abc123def", session.IdentifiedEmailHash);
        Assert.Equal("carrier", session.IdentifiedRole);
    }

    // ── H-8_03 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H8_03_UtmBuilderAdmin_Anonymous_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var res = await client.GetAsync("/Admin/Reports/UtmBuilder");
        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther);
    }

    // ── H-9f_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9f_01_Html_Has_NoCache_Headers()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/");
        res.EnsureSuccessStatusCode();
        var headers = new List<string>();
        if (res.Headers.TryGetValues("Cache-Control", out var a)) headers.AddRange(a);
        if (res.Content.Headers.TryGetValues("Cache-Control", out var b)) headers.AddRange(b);
        var cc = string.Join(",", headers);
        Assert.Contains("no-cache", cc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", cc, StringComparison.OrdinalIgnoreCase);
    }

    // ── H-7_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H7_01_CmdK_Served()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/js/admin/cmdk.js");
        res.EnsureSuccessStatusCode();
        var js = await res.Content.ReadAsStringAsync();
        Assert.Contains("ROUTES", js);
        Assert.Contains("/Admin/Reports/RealTime", js);
        Assert.Contains("metaKey", js);
    }

    // ── H-6_01 ─────────────────────────────────────────────────────────
    [Fact]
    public void H6_01_SessionDetail_Markup_ContainsJumpToConsentScript()
    {
        var here = AppContext.BaseDirectory;
        var detailPath = "/Users/ryanjustin/projects/SWitchboardWebsite/src/TheSwitchboard.Web/Pages/Admin/Reports/Sessions/Detail.cshtml";
        if (!File.Exists(detailPath))
        {
            // Walk up to solution root
            var dir = new DirectoryInfo(here);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TheSwitchboardWeb.sln"))) dir = dir.Parent;
            if (dir != null) detailPath = Path.Combine(dir.FullName, "src", "TheSwitchboard.Web", "Pages", "Admin", "Reports", "Sessions", "Detail.cshtml");
        }
        var text = File.ReadAllText(detailPath);
        Assert.Contains("player.goto(", text);
        Assert.Contains("Jump to form submit", text);
    }

    // ── H-4_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H4_01_Insights_ReadFromRollup_WithoutRawEvents()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Wipe any residual rollup rows for the test window.
        var today = DateTime.UtcNow.Date;
        var existing = db.EventRollupDailies.Where(r => r.Metric == "pageviews" && r.Date >= today.AddDays(-10));
        db.EventRollupDailies.RemoveRange(existing);
        await db.SaveChangesAsync();

        // Seed 7 days of flat baseline (10/day) + today spike to 150.
        for (var i = 1; i <= 7; i++)
            db.EventRollupDailies.Add(new EventRollupDaily { Date = today.AddDays(-i), Path = "/", Metric = "pageviews", Dimension = "", Value = 10 });
        db.EventRollupDailies.Add(new EventRollupDaily { Date = today, Path = "/", Metric = "pageviews", Dimension = "", Value = 150 });
        await db.SaveChangesAsync();

        var svc = scope.ServiceProvider.GetRequiredService<IInsightsService>();
        var found = await svc.DetectAsync();
        Assert.Contains(found, i => i.Metric == "pageviews");
    }

    // ── H-3_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H3_01_TrackerEndpoint_Over300PerBucket_Returns429()
    {
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var gotTooMany = false;
        for (var i = 0; i < 320; i++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/ping");
            req.Headers.Add("Origin", "http://localhost");
            req.Content = new StringContent("{\"vid\":\"x\",\"sid\":\"y\"}", Encoding.UTF8, "application/json");
            var res = await client.SendAsync(req);
            if (res.StatusCode == (HttpStatusCode)429) { gotTooMany = true; break; }
        }
        Assert.True(gotTooMany, "Expected a 429 after crossing the 300-per-bucket tracker cap");
    }

    // ── H-2_02 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H2_02_DefaultAlertRulesSeeder_AddsThreeDefaults()
    {
        var seeder = new DefaultAlertRulesSeeder(
            _factory.Services,
            _factory.Services.GetRequiredService<ILogger<DefaultAlertRulesSeeder>>());
        await seeder.StartAsync(CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.AlertRules.AnyAsync(r => r.Name == "capture-rate-below-95"));
        Assert.True(await db.AlertRules.AnyAsync(r => r.Name == "bot-rate-above-5"));
        Assert.True(await db.AlertRules.AnyAsync(r => r.Name == "js-errors-above-20"));
    }
}
