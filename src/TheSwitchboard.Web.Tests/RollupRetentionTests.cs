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
/// Slice T-10 — Rollups + retention + exports.
///
///   T10_01 — RollupRunner aggregates raw pageviews + sessions + clicks into
///            EventRollupDaily rows for a given calendar day.
///   T10_02 — RetentionRunner purges ClickEvent / PageView older than 90 days,
///            soft-deletes ReplayChunk.Payload past 1 year. Visitor + Session
///            + Goal tables are preserved indefinitely.
///   T10_03 — ExportService streams a CSV whose row count matches the DB query.
///   T10_04 — /Admin/Reports/Exports anon → 302 /Admin/Login.
///   T10_05 — Running RollupRunner twice for the same date is idempotent.
/// </summary>
public class RollupRetentionTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RollupRetentionTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ── T10_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T10_01_RollupRunner_AggregatesDay()
    {
        var day = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            for (var i = 0; i < 3; i++)
                db.PageViews.Add(new PageView { Path = "/", Timestamp = day.AddHours(i), SessionId = $"s_{i}" });
            for (var i = 0; i < 2; i++)
                db.Sessions.Add(new Session { Id = $"t10s_{Guid.NewGuid():N}", StartedAt = day.AddHours(i), EndedAt = day.AddHours(i).AddMinutes(2), IsBot = false });
            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IRollupRunner>();
        await runner.RollupDayAsync(day);

        using var verifyScope = _factory.Services.CreateScope();
        var vdb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await vdb.EventRollupDailies.Where(r => r.Date == day.Date).ToListAsync();
        var pvRow = rows.FirstOrDefault(r => r.Metric == "pageviews" && r.Path == "/");
        Assert.NotNull(pvRow);
        Assert.True(pvRow!.Value >= 3);
        var sessRow = rows.FirstOrDefault(r => r.Metric == "sessions");
        Assert.NotNull(sessRow);
        Assert.True(sessRow!.Value >= 2);
    }

    // ── T10_02 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T10_02_RetentionRunner_Purges90DayOldRawEvents()
    {
        var now = DateTime.UtcNow;
        var oldDate = now.AddDays(-100);
        var fresh = now.AddDays(-5);
        var oldClickId = "t10_old_click_" + Guid.NewGuid().ToString("N")[..8];

        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ClickEvents.Add(new ClickEvent { SessionId = oldClickId, Path = "/", Ts = oldDate, Selector = "a" });
            db.ClickEvents.Add(new ClickEvent { SessionId = "t10_fresh_" + Guid.NewGuid().ToString("N")[..8], Path = "/", Ts = fresh, Selector = "a" });
            db.PageViews.Add(new PageView { Path = "/old", Timestamp = oldDate });
            db.PageViews.Add(new PageView { Path = "/new", Timestamp = fresh });
            // Visitors are NOT purged — ensure this one survives.
            db.Visitors.Add(new Visitor { Id = "t10_vis_keep", FirstSeen = oldDate, LastSeen = oldDate });
            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IRetentionRunner>();
        await runner.RunAsync(now);

        using var verifyScope = _factory.Services.CreateScope();
        var db2 = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db2.ClickEvents.AnyAsync(c => c.SessionId == oldClickId));
        Assert.True(await db2.PageViews.AnyAsync(p => p.Path == "/new"));
        Assert.False(await db2.PageViews.AnyAsync(p => p.Path == "/old"));
        Assert.True(await db2.Visitors.AnyAsync(v => v.Id == "t10_vis_keep"));
    }

    // ── T10_03 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T10_03_ExportService_Sessions_Csv()
    {
        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            for (var i = 0; i < 4; i++)
                db.Sessions.Add(new Session { Id = $"t10x_{Guid.NewGuid():N}", StartedAt = DateTime.UtcNow.AddMinutes(-i), EndedAt = DateTime.UtcNow, PageCount = i });
            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IExportService>();
        using var ms = new MemoryStream();
        var rows = await svc.WriteSessionsCsvAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), ms);
        Assert.True(rows >= 4);
        var csv = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Header + rows.
        Assert.Equal(rows + 1, lines.Length);
        Assert.Contains("SessionId", lines[0]);
    }

    // ── T10_04 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T10_04_ExportsAdmin_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/Exports");
        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther);
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ── T10_05 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T10_05_RollupRunner_Idempotent()
    {
        var day = new DateTime(2025, 11, 3, 0, 0, 0, DateTimeKind.Utc);
        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PageViews.Add(new PageView { Path = "/idem", Timestamp = day.AddHours(2) });
            await db.SaveChangesAsync();
        }

        using (var s1 = _factory.Services.CreateScope())
            await s1.ServiceProvider.GetRequiredService<IRollupRunner>().RollupDayAsync(day);
        using (var s2 = _factory.Services.CreateScope())
            await s2.ServiceProvider.GetRequiredService<IRollupRunner>().RollupDayAsync(day);

        using var scope = _factory.Services.CreateScope();
        var db2 = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db2.EventRollupDailies.CountAsync(r => r.Date == day.Date && r.Path == "/idem" && r.Metric == "pageviews");
        Assert.Equal(1, count);
    }
}
