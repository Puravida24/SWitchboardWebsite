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
/// Slice T-3 — Sessions, Signals, Bot Classification.
///
///   T3_01 — 5 pageviews on the same sid collapse into ONE Session row (PageCount=5).
///   T3_02 — Two distinct sids create two Session rows.
///   T3_03 — HeadlessChrome UA flags IsBot=true with BotReason containing "headless".
///   T3_04 — Legit iPhone Safari UA does NOT flag IsBot.
///   T3_05 — POST /api/tracking/signals persists exactly one BrowserSignal row per sid,
///           even if the client posts twice (idempotent).
///   T3_06 — /api/tracking/signals rejects foreign Origin with 403.
///   T3_07 — /Admin/Reports/Sessions/Index redirects anon to /Admin/Login.
/// </summary>
public class SessionTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SessionTests(SwitchboardWebApplicationFactory factory)
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

    private async Task<HttpResponseMessage> PostPageview(object body, string origin = "http://localhost")
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/pageview");
        req.Headers.Add("Origin", origin);
        req.Content = Json(body);
        return await _client.SendAsync(req);
    }

    private async Task<HttpResponseMessage> PostSignals(object body, string origin = "http://localhost")
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/signals");
        req.Headers.Add("Origin", origin);
        req.Content = Json(body);
        return await _client.SendAsync(req);
    }

    // ── T3_01 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T3_01_FivePageviewsSameSidCollapseToOneSession()
    {
        var sid = "t3_sid_" + Guid.NewGuid().ToString("N")[..12];
        var vid = "t3_vid_" + Guid.NewGuid().ToString("N")[..12];
        var ua = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_0) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15";

        for (var i = 0; i < 5; i++)
        {
            var res = await PostPageview(new { vid, sid, path = $"/p{i}", userAgent = ua });
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sessions = await db.Sessions.Where(s => s.Id == sid).ToListAsync();
        Assert.Single(sessions);
        Assert.Equal(5, sessions[0].PageCount);
        Assert.Equal(vid, sessions[0].VisitorId);
        Assert.Equal("desktop", sessions[0].DeviceType);
        Assert.Equal("Safari", sessions[0].Browser);
        Assert.Equal("macOS", sessions[0].Os);
    }

    // ── T3_02 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T3_02_TwoDistinctSidsCreateTwoSessions()
    {
        var vid = "t3_vid_" + Guid.NewGuid().ToString("N")[..12];
        var sidA = "t3_sidA_" + Guid.NewGuid().ToString("N")[..12];
        var sidB = "t3_sidB_" + Guid.NewGuid().ToString("N")[..12];

        (await PostPageview(new { vid, sid = sidA, path = "/" })).EnsureSuccessStatusCode();
        (await PostPageview(new { vid, sid = sidB, path = "/" })).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ids = await db.Sessions.Where(s => s.Id == sidA || s.Id == sidB).Select(s => s.Id).ToListAsync();
        Assert.Contains(sidA, ids);
        Assert.Contains(sidB, ids);
        Assert.Equal(2, ids.Count);
    }

    // ── T3_03 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T3_03_HeadlessChromeUaFlagsBot()
    {
        var sid = "t3_bot_" + Guid.NewGuid().ToString("N")[..12];
        var ua = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/123.0.6312.86 Safari/537.36";

        (await PostPageview(new { vid = "b", sid, path = "/", userAgent = ua })).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Sessions.FirstOrDefaultAsync(s => s.Id == sid);
        Assert.NotNull(row);
        Assert.True(row!.IsBot, "HeadlessChrome should be classified as bot");
        Assert.False(string.IsNullOrEmpty(row.BotReason));
        Assert.Contains("headless", row.BotReason!, StringComparison.OrdinalIgnoreCase);
    }

    // ── T3_04 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T3_04_LegitiPhoneSafariDoesNotFlagBot()
    {
        var sid = "t3_ok_" + Guid.NewGuid().ToString("N")[..12];
        var ua = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";

        (await PostPageview(new { vid = "h", sid, path = "/", userAgent = ua })).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Sessions.FirstOrDefaultAsync(s => s.Id == sid);
        Assert.NotNull(row);
        Assert.False(row!.IsBot);
        Assert.Equal("mobile", row.DeviceType);
    }

    // ── T3_05 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T3_05_SignalsPersistedOncePerSession_Idempotent()
    {
        var sid = "t3_sig_" + Guid.NewGuid().ToString("N")[..12];
        var vid = "t3_vid_" + Guid.NewGuid().ToString("N")[..12];

        // Parent session must exist first (real client flow posts pageview before signals).
        (await PostPageview(new { vid, sid, path = "/", userAgent = "Mozilla/5.0" })).EnsureSuccessStatusCode();

        var payload = new
        {
            vid, sid,
            timezone = "America/New_York",
            language = "en-US",
            colorDepth = 24,
            hardwareConcurrency = 8,
            deviceMemory = 8,
            touchPoints = 0,
            screenW = 1920, screenH = 1080,
            pixelRatio = 2.0,
            cookies = true, localStorage = true, sessionStorage = true,
            isMetaWebview = false,
            canvasFingerprint = "stub-abc",
            webGLVendor = "Apple Inc.",
            webGLRenderer = "Apple GPU"
        };

        (await PostSignals(payload)).EnsureSuccessStatusCode();
        (await PostSignals(payload)).EnsureSuccessStatusCode(); // second post — still one row

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Set<BrowserSignal>().CountAsync(b => b.SessionId == sid);
        Assert.Equal(1, count);

        var row = await db.Set<BrowserSignal>().FirstAsync(b => b.SessionId == sid);
        Assert.Equal("America/New_York", row.Timezone);
        Assert.Equal(1920, row.ScreenW);
        Assert.Equal(8, row.HardwareConcurrency);
    }

    // ── T3_06 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T3_06_Signals_ForeignOrigin_Returns403()
    {
        var res = await PostSignals(new { vid = "x", sid = "y" }, origin: "https://evil.example.com");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── T3_07 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T3_07_SessionsAdmin_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/Sessions");
        Assert.True(
            res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"Expected redirect, got {(int)res.StatusCode}");
        var loc = res.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/Admin/Login", loc, StringComparison.OrdinalIgnoreCase);
    }
}
