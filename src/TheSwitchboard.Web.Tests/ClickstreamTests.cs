using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice T-4 — Clickstream + Rage/Dead detection.
///
///   T4_01 — Three POSTs for the same (sid, selector) within 500ms of each other →
///           all three ClickEvent rows end up with IsRage=true.
///   T4_02 — A click payload marked isDead=true persists with IsDead=true.
///   T4_03 — A normal isolated click persists with IsRage=false, IsDead=false.
///   T4_04 — clickstream.js served; contains MutationObserver + visibilitychange
///           + a selector-building helper.
///   T4_05 — /api/tracking/clicks rejects foreign Origin with 403.
///   T4_06 — 501st click on the same session is silently dropped (cap at 500).
///   T4_07 — /Admin/Reports/Frustration redirects anon.
///   T4_08 — /Admin/Reports/Heatmaps/Click redirects anon.
/// </summary>
public class ClickstreamTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClickstreamTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private async Task<HttpResponseMessage> PostClicks(object body, string origin = "http://localhost")
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/clicks");
        req.Headers.Add("Origin", origin);
        req.Content = Json(body);
        return await _client.SendAsync(req);
    }

    private static object BuildClick(string sid, string selector, DateTime ts, bool isDead = false, string path = "/") => new
    {
        sid,
        vid = "t4vid",
        path,
        ts,
        x = 100,
        y = 120,
        viewportW = 1920,
        viewportH = 1080,
        pageW = 1920,
        pageH = 3000,
        selector,
        tagName = "button",
        elementText = "Subscribe",
        elementHref = (string?)null,
        mouseButton = 0,
        isDead
    };

    // ── T4_01 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T4_01_ThreeClicksSameSelector_Under500ms_AllFlaggedRage()
    {
        var sid = "t4_sid_" + Guid.NewGuid().ToString("N")[..12];
        var sel = "#cta-primary";
        var baseTs = DateTime.UtcNow;
        var clicks = new[]
        {
            BuildClick(sid, sel, baseTs),
            BuildClick(sid, sel, baseTs.AddMilliseconds(120)),
            BuildClick(sid, sel, baseTs.AddMilliseconds(340))
        };
        var res = await PostClicks(new { clicks });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.Set<ClickEvent>()
            .Where(c => c.SessionId == sid && c.Selector == sel)
            .OrderBy(c => c.Ts)
            .ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.True(r.IsRage, "every click in the rage burst should be flagged"));
    }

    // ── T4_02 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T4_02_IsDeadClick_Persists()
    {
        var sid = "t4_dead_" + Guid.NewGuid().ToString("N")[..12];
        var click = BuildClick(sid, "span.inert", DateTime.UtcNow, isDead: true);
        var res = await PostClicks(new { clicks = new[] { click } });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Set<ClickEvent>().FirstOrDefaultAsync(c => c.SessionId == sid);
        Assert.NotNull(row);
        Assert.True(row!.IsDead);
        Assert.False(row.IsRage);
    }

    // ── T4_03 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T4_03_IsolatedClick_NoFlags()
    {
        var sid = "t4_lone_" + Guid.NewGuid().ToString("N")[..12];
        var click = BuildClick(sid, "nav a.home", DateTime.UtcNow);
        var res = await PostClicks(new { clicks = new[] { click } });
        res.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Set<ClickEvent>().FirstOrDefaultAsync(c => c.SessionId == sid);
        Assert.NotNull(row);
        Assert.False(row!.IsRage);
        Assert.False(row.IsDead);
    }

    // ── T4_04 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T4_04_ClickstreamJs_IsServedAndHasCoreHooks()
    {
        var res = await _client.GetAsync("/js/tracker/clickstream.js");
        res.EnsureSuccessStatusCode();
        var js = await res.Content.ReadAsStringAsync();
        Assert.Contains("/api/tracking/clicks", js);
        Assert.Contains("MutationObserver", js);
        Assert.Contains("visibilitychange", js);
        Assert.Contains("selector", js, StringComparison.OrdinalIgnoreCase);
    }

    // ── T4_05 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T4_05_Clicks_ForeignOrigin_Returns403()
    {
        var sid = "foreign_" + Guid.NewGuid().ToString("N")[..8];
        var res = await PostClicks(new { clicks = new[] { BuildClick(sid, "a", DateTime.UtcNow) } }, origin: "https://evil.example.com");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── T4_06 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T4_06_Cap500PerSession_ExtraDropped()
    {
        var sid = "t4_cap_" + Guid.NewGuid().ToString("N")[..12];
        // Post 600 — server should store only the first 500.
        var baseTs = DateTime.UtcNow;
        var batch1 = Enumerable.Range(0, 300)
            .Select(i => BuildClick(sid, "button#" + i, baseTs.AddMilliseconds(i)))
            .ToArray();
        var batch2 = Enumerable.Range(300, 300)
            .Select(i => BuildClick(sid, "button#" + i, baseTs.AddMilliseconds(i)))
            .ToArray();
        (await PostClicks(new { clicks = batch1 })).EnsureSuccessStatusCode();
        (await PostClicks(new { clicks = batch2 })).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Set<ClickEvent>().CountAsync(c => c.SessionId == sid);
        Assert.Equal(500, count);
    }

    // ── T4_07 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T4_07_FrustrationAdmin_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/Frustration");
        Assert.True(
            res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"Expected redirect, got {(int)res.StatusCode}");
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ── T4_08 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T4_08_ClickHeatmapAdmin_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/Heatmaps/Click");
        Assert.True(
            res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"Expected redirect, got {(int)res.StatusCode}");
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
