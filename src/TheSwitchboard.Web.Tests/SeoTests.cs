using Microsoft.AspNetCore.Mvc.Testing;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// H-9 SEO hardening. Verifies schema markup + per-page meta + canonical +
/// sitemap expansion + IndexNow bits.
/// </summary>
public class SeoTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SeoTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient();
    }

    // ── H9a_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9a_01_Homepage_Contains_SoftwareApplication_Schema()
    {
        var html = await _client.GetStringAsync("/");
        Assert.Contains("\"@type\":\"SoftwareApplication\"", html);
        Assert.Contains("\"applicationCategory\":\"BusinessApplication\"", html);
    }

    // ── H9a_02 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9a_02_Homepage_Contains_Service_Schema()
    {
        var html = await _client.GetStringAsync("/");
        Assert.Contains("\"@type\":\"Service\"", html);
        Assert.Contains("\"areaServed\"", html);
    }

    // ── H9a_03 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9a_03_Organization_Has_SameAs_FoundingDate_Founder()
    {
        var html = await _client.GetStringAsync("/");
        Assert.Contains("\"sameAs\"", html);
        Assert.Contains("\"foundingDate\"", html);
        Assert.Contains("\"founder\"", html);
    }

    // ── H9i_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9i_01_Pages_Use_LangEnUS()
    {
        foreach (var p in new[] { "/", "/privacy", "/terms", "/accessibility" })
        {
            var html = await _client.GetStringAsync(p);
            Assert.Contains("<html lang=\"en-US\">", html);
        }
    }

    // ── H9i_02 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9i_02_Pages_Reference_Manifest()
    {
        foreach (var p in new[] { "/", "/privacy", "/terms", "/accessibility" })
        {
            var html = await _client.GetStringAsync(p);
            Assert.Contains("rel=\"manifest\"", html);
            Assert.Contains("/manifest.webmanifest", html);
        }
    }

    // ── H9i_03 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9i_03_Manifest_Served()
    {
        var res = await _client.GetAsync("/manifest.webmanifest");
        Assert.True(res.IsSuccessStatusCode, $"manifest must be served (got {(int)res.StatusCode})");
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("\"short_name\"", body);
    }

    // ── H9g_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9g_01_PublicPages_Use_NewOgImage()
    {
        foreach (var p in new[] { "/", "/privacy", "/terms", "/accessibility" })
        {
            var html = await _client.GetStringAsync(p);
            Assert.Contains("/wireframes/assets/og/og-default.png", html);
        }
    }

    // ── H9g_02 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9g_02_OgImage_IsServed()
    {
        var res = await _client.GetAsync("/wireframes/assets/og/og-default.png");
        res.EnsureSuccessStatusCode();
        Assert.Equal("image/png", res.Content.Headers.ContentType?.MediaType);
        var len = res.Content.Headers.ContentLength ?? 0;
        Assert.InRange(len, 10_000, 200_000);
    }

    // ── H9b_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9b_01_Homepage_Has_Hreflang()
    {
        var html = await _client.GetStringAsync("/");
        Assert.Contains("hreflang=\"en-us\"", html);
        Assert.Contains("hreflang=\"x-default\"", html);
    }

    // ── H9b_02 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9b_02_LegalPages_Have_Breadcrumbs()
    {
        foreach (var p in new[] { "/privacy", "/terms", "/accessibility" })
        {
            var html = await _client.GetStringAsync(p);
            Assert.Contains("\"@type\":\"BreadcrumbList\"", html);
            Assert.Contains(p, html);
        }
    }

    // ── H9b_03 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9b_03_LegalPages_Have_DistinctTitles()
    {
        foreach (var (path, expected) in new[] {
            ("/privacy",       "Privacy"),
            ("/terms",         "Terms"),
            ("/accessibility", "Accessibility") })
        {
            var html = await _client.GetStringAsync(path);
            Assert.Contains($"<title>{expected}", html);
        }
    }

    // ── H9a_04 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task H9a_04_Homepage_Does_Not_Contain_Deprecated_SearchAction()
    {
        var html = await _client.GetStringAsync("/");
        // Google deprecated sitelinks search box; dropping SearchAction.
        Assert.DoesNotContain("\"@type\":\"SearchAction\"", html);
    }
}
