using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice T-1 — Tracker Foundation &amp; Identity.
///
/// RED phase: every test here fails until T-1 implementation lands.
///
///   T1_01 — Homepage ships the tracker script tag with a nonce matching the CSP header.
///   T1_02 — The tracker JS respects DNT (early-exits before firing any ping).
///   T1_03 — The tracker JS sets sw_vid + sw_sid cookies (via document.cookie — exercised by
///           inspection of the script content since we can't run a headless browser here).
///   T1_04 — POST /api/tracking/ping with valid Origin returns 204.
///   T1_05 — POST /api/tracking/ping with foreign Origin is rejected (403).
///   T1_06 — /Admin/Reports/Health redirects to login when unauthenticated.
/// </summary>
public class TrackerFoundationTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TrackerFoundationTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ── T1_01 ───────────────────────────────────────────────────────────
    // The home page must inject <script nonce="…" src="/js/tracker/tracker.js?v=…" defer></script>
    // and the nonce must match the CSP header's script-src nonce.
    [Fact]
    public async Task T1_01_Homepage_IncludesTrackerScriptWithNonceMatchingCsp()
    {
        var res = await _client.GetAsync("/");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();

        var csp = res.Headers.GetValues("Content-Security-Policy").First();
        var cspNonceMatch = Regex.Match(csp, @"'nonce-([^']+)'");
        Assert.True(cspNonceMatch.Success, "CSP header should contain nonce-…");
        var nonce = cspNonceMatch.Groups[1].Value;

        var tagPattern = $"<script[^>]+src=\"/js/tracker/tracker\\.js\\?v=[^\"]+\"[^>]*nonce=\"{Regex.Escape(nonce)}\"[^>]*>|" +
                         $"<script[^>]+nonce=\"{Regex.Escape(nonce)}\"[^>]+src=\"/js/tracker/tracker\\.js\\?v=[^\"]+\"[^>]*>";
        Assert.Matches(tagPattern, body);
    }

    // ── T1_02 ───────────────────────────────────────────────────────────
    // tracker.js itself must contain an early-exit guard for DNT / GPC so we never even attempt
    // a network call from a signalling visitor.
    [Fact]
    public async Task T1_02_TrackerJs_HasDntAndGpcEarlyExit()
    {
        var res = await _client.GetAsync("/js/tracker/tracker.js");
        res.EnsureSuccessStatusCode();
        var js = await res.Content.ReadAsStringAsync();

        Assert.Contains("doNotTrack", js);
        Assert.Contains("globalPrivacyControl", js);
    }

    // ── T1_03 ───────────────────────────────────────────────────────────
    // identity.js must assign sw_vid + sw_sid cookies with correct attributes and
    // triple-write to localStorage + sessionStorage for ITP resilience.
    [Fact]
    public async Task T1_03_IdentityJs_SetsVisitorAndSessionIdsWithTripleWrite()
    {
        var res = await _client.GetAsync("/js/tracker/identity.js");
        res.EnsureSuccessStatusCode();
        var js = await res.Content.ReadAsStringAsync();

        Assert.Contains("sw_vid", js);
        Assert.Contains("sw_sid", js);
        Assert.Contains("SameSite=Lax", js);
        Assert.Contains("localStorage", js);
        Assert.Contains("sessionStorage", js);
    }

    // ── T1_04 ───────────────────────────────────────────────────────────
    // POST /api/tracking/ping with same-origin Origin header returns 204.
    [Fact]
    public async Task T1_04_Ping_SameOrigin_Returns204()
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/ping");
        req.Headers.Add("Origin", "http://localhost");
        req.Content = new StringContent("{\"vid\":\"sw_vid_test\",\"sid\":\"sw_sid_test\"}", System.Text.Encoding.UTF8, "application/json");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    // ── T1_05 ───────────────────────────────────────────────────────────
    // Foreign Origin is rejected — CSRF defense reused from ContactEndpoints.
    [Fact]
    public async Task T1_05_Ping_ForeignOrigin_Returns403()
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/ping");
        req.Headers.Add("Origin", "https://evil.example.com");
        req.Content = new StringContent("{\"vid\":\"x\",\"sid\":\"y\"}", System.Text.Encoding.UTF8, "application/json");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── T1_06 ───────────────────────────────────────────────────────────
    // /Admin/Reports/Health must redirect anonymous visitors to login.
    [Fact]
    public async Task T1_06_HealthAdminPage_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/Health");
        Assert.True(
            res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"Expected redirect, got {(int)res.StatusCode}");
        var location = res.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/Admin/Login", location, StringComparison.OrdinalIgnoreCase);
    }
}
