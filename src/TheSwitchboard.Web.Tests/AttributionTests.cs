using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Analytics;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice T-2 — Attribution + Enriched PageView.
///
/// RED phase: these tests fail until T-2 implementation lands.
///
///   T2_01 — POST /api/tracking/pageview with UTM + gclid → PageView row with all
///           three populated + LandingFlag=true on the first PV of a session.
///   T2_02 — Second pageview on the same sid → LandingFlag=false, attribution still
///           attached from the landing row.
///   T2_03 — UA "Mozilla/5.0 (iPhone…) Safari/605" → PageView.DeviceType="mobile",
///           Browser="Safari", Os="iOS".
///   T2_04 — pageview.js is served and references attribution + viewport.
///   T2_05 — attribution.js is served and parses the 8 attribution params.
///   T2_06 — Foreign-origin POST /api/tracking/pageview → 403.
///   T2_07 — /Admin/Reports/Attribution redirects anonymous to /Admin/Login.
/// </summary>
public class AttributionTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AttributionTests(SwitchboardWebApplicationFactory factory)
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

    // ── T2_01 ───────────────────────────────────────────────────────────
    [Fact]
    public async Task T2_01_UtmAndGclidPersistedOnPageView()
    {
        var sid = "t2_sid_" + Guid.NewGuid().ToString("N")[..12];
        var vid = "t2_vid_" + Guid.NewGuid().ToString("N")[..12];
        var res = await PostPageview(new
        {
            vid,
            sid,
            path = "/",
            utmSource = "linkedin",
            utmMedium = "social",
            utmCampaign = "hero",
            utmTerm = "switchboard",
            utmContent = "banner",
            gclid = "abc123",
            fbclid = "fb123",
            msclkid = "ms123",
            viewportW = 1920,
            viewportH = 1080,
            userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_0) AppleWebKit/605 Safari/605",
            ts = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pv = await db.PageViews.FirstOrDefaultAsync(p => p.SessionId == sid);
        Assert.NotNull(pv);
        Assert.Equal("linkedin", pv!.UtmSource);
        Assert.Equal("social", pv.UtmMedium);
        Assert.Equal("hero", pv.UtmCampaign);
        Assert.Equal("switchboard", pv.UtmTerm);
        Assert.Equal("banner", pv.UtmContent);
        Assert.Equal("abc123", pv.Gclid);
        Assert.Equal("fb123", pv.Fbclid);
        Assert.Equal("ms123", pv.Msclkid);
        Assert.True(pv.LandingFlag);
        Assert.Equal(1920, pv.ViewportW);
        Assert.Equal(1080, pv.ViewportH);
    }

    // ── T2_02 ───────────────────────────────────────────────────────────
    [Fact]
    public async Task T2_02_LandingFlagOnlyOnFirstPageviewInSession()
    {
        var sid = "t2_sid_" + Guid.NewGuid().ToString("N")[..12];
        var vid = "t2_vid_" + Guid.NewGuid().ToString("N")[..12];

        var res1 = await PostPageview(new
        {
            vid, sid, path = "/",
            utmSource = "linkedin", utmCampaign = "launch",
            userAgent = "Mozilla/5.0"
        });
        Assert.Equal(HttpStatusCode.NoContent, res1.StatusCode);

        var res2 = await PostPageview(new
        {
            vid, sid, path = "/about",
            userAgent = "Mozilla/5.0"
        });
        Assert.Equal(HttpStatusCode.NoContent, res2.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.PageViews
            .Where(p => p.SessionId == sid)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.True(rows[0].LandingFlag, "First PV in session should be Landing");
        Assert.False(rows[1].LandingFlag, "Second PV in session should NOT be Landing");
        Assert.Equal("linkedin", rows[0].UtmSource);
    }

    // ── T2_03 ───────────────────────────────────────────────────────────
    [Fact]
    public async Task T2_03_UserAgentParsedToDeviceBrowserOs()
    {
        var sid = "t2_sid_" + Guid.NewGuid().ToString("N")[..12];
        var ua = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
        var res = await PostPageview(new { vid = "v", sid, path = "/", userAgent = ua });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pv = await db.PageViews.FirstOrDefaultAsync(p => p.SessionId == sid);
        Assert.NotNull(pv);
        Assert.Equal("mobile", pv!.DeviceType);
        Assert.Equal("Safari", pv.Browser);
        Assert.Equal("iOS", pv.Os);
    }

    // ── T2_04 ───────────────────────────────────────────────────────────
    [Fact]
    public async Task T2_04_PageviewJs_IsServedAndReferencesAttributionAndViewport()
    {
        var res = await _client.GetAsync("/js/tracker/pageview.js");
        res.EnsureSuccessStatusCode();
        var js = await res.Content.ReadAsStringAsync();
        Assert.Contains("/api/tracking/pageview", js);
        Assert.Contains("viewport", js, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("userAgent", js);
    }

    // ── T2_05 ───────────────────────────────────────────────────────────
    [Fact]
    public async Task T2_05_AttributionJs_IsServedAndParsesEightParams()
    {
        var res = await _client.GetAsync("/js/tracker/attribution.js");
        res.EnsureSuccessStatusCode();
        var js = await res.Content.ReadAsStringAsync();
        Assert.Contains("utm_source", js);
        Assert.Contains("utm_medium", js);
        Assert.Contains("utm_campaign", js);
        Assert.Contains("utm_term", js);
        Assert.Contains("utm_content", js);
        Assert.Contains("gclid", js);
        Assert.Contains("fbclid", js);
        Assert.Contains("msclkid", js);
        Assert.Contains("sessionStorage", js);
    }

    // ── T2_06 ───────────────────────────────────────────────────────────
    [Fact]
    public async Task T2_06_Pageview_ForeignOrigin_Returns403()
    {
        var res = await PostPageview(new { vid = "x", sid = "y", path = "/" }, origin: "https://evil.example.com");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── T2_07 ───────────────────────────────────────────────────────────
    [Fact]
    public async Task T2_07_AttributionAdminPage_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/Attribution");
        Assert.True(
            res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"Expected redirect, got {(int)res.StatusCode}");
        var location = res.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/Admin/Login", location, StringComparison.OrdinalIgnoreCase);
    }
}
