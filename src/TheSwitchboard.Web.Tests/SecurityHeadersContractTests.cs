using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// A10 — contract tests asserting the security-header envelope on every route
/// class: public pages, admin pages, API endpoints, and /verify/*.
///
/// Prior tests sample only "/" for headers, so a future regression on
/// /privacy or /Admin/Dashboard would slip through. This file locks down:
///
///   Universal (every 2xx/3xx response):
///     X-Content-Type-Options:  nosniff
///     X-Frame-Options:         SAMEORIGIN
///     X-XSS-Protection:        1; mode=block
///     Referrer-Policy:         strict-origin-when-cross-origin
///     Permissions-Policy:      camera=(), microphone=(), geolocation=()
///     Content-Security-Policy: includes script-src 'self' 'nonce-XXX'
///                              + frame-ancestors 'self'
///
///   Admin + API + Verify (never indexable):
///     X-Robots-Tag:            noindex, nofollow
///
///   Public pages (ARE indexable):
///     X-Robots-Tag:            MUST be absent or not noindex
/// </summary>
public class SecurityHeadersContractTests : IClassFixture<Slice4Factory>
{
    private readonly Slice4Factory _factory;
    public SecurityHeadersContractTests(Slice4Factory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
    }

    // Every non-static public page must emit the full universal envelope.
    [Theory]
    [InlineData("/")]
    [InlineData("/privacy")]
    [InlineData("/terms")]
    [InlineData("/accessibility")]
    public async Task A10_01_PublicPage_HasUniversalSecurityHeaders(string path)
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync(path);
        Assert.True(res.IsSuccessStatusCode, $"GET {path} responded {(int)res.StatusCode}");

        AssertUniversalHeaders(res, path);
        AssertNotFlaggedNoindex(res, path); // public pages must be indexable
    }

    // Admin UI pages (including /Admin/Login which is anonymous) always carry
    // X-Robots-Tag: noindex, nofollow — search engines must not index them
    // even if someone external links in.
    [Theory]
    [InlineData("/Admin/Login")]
    [InlineData("/Admin/Dashboard")]
    [InlineData("/Admin/Reports/Overview")]
    [InlineData("/Admin/Submissions")]
    public async Task A10_02_AdminPage_HasNoindexAndUniversalHeaders(string path)
    {
        var authed = await _factory.LoggedInClientAsync();
        var res = await authed.GetAsync(path);
        // Accept any 2xx/3xx — /Admin/Login is 200, others are 200 after auth.
        Assert.True((int)res.StatusCode < 400, $"GET {path} responded {(int)res.StatusCode}");

        AssertUniversalHeaders(res, path);
        AssertFlaggedNoindex(res, path);
    }

    // API routes should be noindex too — scrapers sometimes follow JSON URLs.
    // Pick a GET-accessible route that doesn't require auth and returns 2xx/4xx.
    [Theory]
    [InlineData("/api/tracking/ping")] // GET → 405 Method Not Allowed is fine; headers still set
    [InlineData("/health")]
    public async Task A10_03_ApiAndHealth_HaveUniversalHeaders(string path)
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync(path);

        AssertUniversalHeaders(res, path);

        // /health is intentionally uncategorized (probe endpoint);
        // /api/* must always be noindex.
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            AssertFlaggedNoindex(res, path);
    }

    // /verify/{id} — shareable public cert URLs. These must NOT dilute search
    // results, so X-Robots-Tag: noindex applies (H-9h).
    [Fact]
    public async Task A10_04_VerifyRoute_IsFlaggedNoindex()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/verify/not-a-real-cert");
        // 404 is expected for the bogus id; header middleware runs before the
        // route handler returns, so X-Robots-Tag should still be set.
        AssertUniversalHeaders(res, "/verify/not-a-real-cert");
        AssertFlaggedNoindex(res, "/verify/not-a-real-cert");
    }

    // CSP nonce must be unique per request — grabbing it twice on the same URL
    // must produce two different values or the XSS protection is fake.
    [Fact]
    public async Task A10_05_CspNonce_ChangesPerRequest()
    {
        var client = _factory.CreateClient();
        var a = NonceFromCsp(await client.GetAsync("/"));
        var b = NonceFromCsp(await client.GetAsync("/"));
        Assert.False(string.IsNullOrEmpty(a), "No nonce on first request.");
        Assert.False(string.IsNullOrEmpty(b), "No nonce on second request.");
        Assert.NotEqual(a, b);
    }

    // ───────────────────────────────────────────────────────────────────
    // helpers

    private static void AssertUniversalHeaders(HttpResponseMessage res, string path)
    {
        string H(string name) =>
            res.Headers.TryGetValues(name, out var v) ? string.Join(",", v) :
            res.Content.Headers.TryGetValues(name, out var v2) ? string.Join(",", v2) : "";

        Assert.Equal("nosniff",                                H("X-Content-Type-Options"));
        Assert.Equal("SAMEORIGIN",                             H("X-Frame-Options"));
        Assert.Equal("1; mode=block",                          H("X-XSS-Protection"));
        Assert.Equal("strict-origin-when-cross-origin",        H("Referrer-Policy"));
        Assert.Equal("camera=(), microphone=(), geolocation=()", H("Permissions-Policy"));

        var csp = H("Content-Security-Policy");
        Assert.False(string.IsNullOrEmpty(csp), $"No CSP on {path}");
        Assert.Matches(@"script-src [^;]*'nonce-[A-Za-z0-9+/=_\-]+'", csp);
        Assert.Contains("frame-ancestors 'self'",               csp);
        Assert.DoesNotContain("'unsafe-eval'",                  csp);
    }

    private static void AssertFlaggedNoindex(HttpResponseMessage res, string path)
    {
        var tag = res.Headers.TryGetValues("X-Robots-Tag", out var v) ? string.Join(",", v) : "";
        Assert.Contains("noindex", tag, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nofollow", tag, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertNotFlaggedNoindex(HttpResponseMessage res, string path)
    {
        var tag = res.Headers.TryGetValues("X-Robots-Tag", out var v) ? string.Join(",", v) : "";
        Assert.False(
            tag.Contains("noindex", StringComparison.OrdinalIgnoreCase),
            $"{path} has X-Robots-Tag '{tag}' — public pages must be indexable.");
    }

    private static string NonceFromCsp(HttpResponseMessage res)
    {
        var csp = res.Headers.TryGetValues("Content-Security-Policy", out var v) ? string.Join(" ", v) : "";
        return Regex.Match(csp, @"'nonce-([A-Za-z0-9+/=_\-]+)'").Groups[1].Value;
    }
}
