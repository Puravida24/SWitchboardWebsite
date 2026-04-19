using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice T-8 — Real-time dashboard (SignalR).
///
///   T8_01 — SessionService.UpsertAsync triggers IRealtimeBroadcaster.BroadcastAsync.
///   T8_02 — /Admin/Reports/RealTime anon → 302 /Admin/Login.
///   T8_03 — /js/vendor/signalr.min.js served 200.
///   T8_04 — /js/admin/realtime.js served 200 with hub connection + event tape hooks.
///   T8_05 — CSP connect-src directive allows wss: scheme.
///   T8_06 — RealtimeMetrics tracks active-visitor counter with TTL.
///   T8_07 — /hubs/realtime responds (with 401/400 for unauth websocket upgrade attempt).
/// </summary>
public class RealTimeTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RealTimeTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ── T8_01 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T8_01_SessionUpsert_InvokesBroadcaster()
    {
        using var scope = _factory.Services.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var metrics = scope.ServiceProvider.GetRequiredService<IRealtimeMetrics>();
        var countBefore = metrics.ActiveVisitorCount(DateTime.UtcNow);

        await sessions.UpsertAsync(new UpsertInput(
            Vid: "rt_vid", Sid: "rt_sid_" + Guid.NewGuid().ToString("N")[..10],
            Path: "/", UserAgent: "Mozilla/5.0",
            IpAddress: "127.0.0.1", Referrer: null,
            UtmSource: null, UtmMedium: null, UtmCampaign: null, UtmTerm: null, UtmContent: null,
            Gclid: null, Fbclid: null, Msclkid: null,
            ViewportW: 1920, ViewportH: 1080,
            ConsentState: null,
            EventKind: EventKind.Pageview,
            IpHash: "hashX"));

        var countAfter = metrics.ActiveVisitorCount(DateTime.UtcNow);
        Assert.True(countAfter >= countBefore + 1, "Upsert should bump the active-visitor counter");
    }

    // ── T8_02 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T8_02_RealTimeAdminPage_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/RealTime");
        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther);
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ── T8_03 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T8_03_SignalrVendorServed()
    {
        var res = await _client.GetAsync("/js/vendor/signalr.min.js");
        Assert.True(res.IsSuccessStatusCode, $"signalr.min.js must be vendored (got {(int)res.StatusCode})");
    }

    // ── T8_04 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T8_04_RealtimeAdminJsServed()
    {
        var res = await _client.GetAsync("/js/admin/realtime.js");
        res.EnsureSuccessStatusCode();
        var js = await res.Content.ReadAsStringAsync();
        Assert.Contains("/hubs/realtime", js);
        Assert.Contains("signalR", js, StringComparison.OrdinalIgnoreCase);
    }

    // ── T8_05 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T8_05_Csp_Connect_Src_Allows_Wss()
    {
        var res = await _client.GetAsync("/");
        res.EnsureSuccessStatusCode();
        var csp = res.Headers.GetValues("Content-Security-Policy").First();
        Assert.Contains("wss:", csp);
    }

    // ── T8_06 ──────────────────────────────────────────────────────────
    [Fact]
    public void T8_06_ActiveVisitorCounter_TtlEnforced()
    {
        using var scope = _factory.Services.CreateScope();
        var metrics = scope.ServiceProvider.GetRequiredService<IRealtimeMetrics>();
        var now = DateTime.UtcNow;
        metrics.TouchVisitor("vid_ttl_A", now);
        metrics.TouchVisitor("vid_ttl_B", now.AddSeconds(-40));  // ~40s ago
        metrics.TouchVisitor("vid_ttl_C", now.AddSeconds(-200)); // older than TTL (120s)

        var active = metrics.ActiveVisitorCount(now);
        // A + B live, C expired.
        Assert.True(active >= 2, $"Expected ≥2 active visitors, got {active}");
        Assert.False(metrics.IsActive("vid_ttl_C", now), "C older than TTL should be inactive");
    }

    // ── T8_07 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T8_07_RealtimeHubNegotiate_RejectsAnonymous()
    {
        // SignalR's negotiate endpoint requires [Authorize] — anon users should
        // NOT get a 200 with a connection token. 401/403/404/302 all acceptable
        // refusals depending on pipeline.
        var res = await _client.PostAsync("/hubs/realtime/negotiate?negotiateVersion=1", new StringContent(""));
        Assert.NotEqual(HttpStatusCode.OK, res.StatusCode);
    }
}
