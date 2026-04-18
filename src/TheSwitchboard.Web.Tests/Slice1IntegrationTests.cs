using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Site;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice 1 BDD scenarios (S1-01 through S1-18) as xUnit integration tests.
/// RED phase: every test in this file should fail until Slice 1 implementation lands.
/// Uses WebApplicationFactory with the default InMemory DB fallback (no DATABASE_URL env var).
/// </summary>
public class Slice1IntegrationTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public Slice1IntegrationTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ── S1-01 Security headers ─────────────────────────────────────────
    [Fact]
    public async Task S1_01_SecurityHeaders_AreSetOnPublicResponses()
    {
        var res = await _client.GetAsync("/");
        Assert.Contains("X-Content-Type-Options", res.Headers.Select(h => h.Key));
        Assert.Contains("X-Frame-Options", res.Headers.Select(h => h.Key));
        Assert.Contains("Referrer-Policy", res.Headers.Select(h => h.Key));
        Assert.Contains("Content-Security-Policy", res.Headers.Select(h => h.Key));
        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").First());
        Assert.Equal("DENY", res.Headers.GetValues("X-Frame-Options").First());
    }

    // ── S1-02 /health ──────────────────────────────────────────────────
    [Fact]
    public async Task S1_02_Health_Returns200()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // ── S1-03 / renders with title + meta ──────────────────────────────
    [Fact]
    public async Task S1_03_Root_RendersTitleAndMeta()
    {
        var res = await _client.GetAsync("/");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("<title>", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Switchboard", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("meta name=\"description\"", body, StringComparison.OrdinalIgnoreCase);
        // The Phoenix visitor scorecard pill should be in the rendered HTML.
        Assert.Contains("scorecard", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── S1-04 In-page nav links ────────────────────────────────────────
    [Fact]
    public async Task S1_04_Nav_ContainsAnchorLinksToKeySections()
    {
        var body = await _client.GetStringAsync("/");
        Assert.Contains("href=\"#platform\"", body);
        Assert.Contains("href=\"#intelligence\"", body);
        Assert.Contains("href=\"#stories\"", body);
        Assert.Contains("href=\"#contact\"", body);
    }

    // ── S1-05/06/07 Legal pages ────────────────────────────────────────
    [Theory]
    [InlineData("/privacy", "Privacy")]
    [InlineData("/terms", "Terms")]
    [InlineData("/accessibility", "Accessibility")]
    public async Task S1_05_06_07_LegalPages_Render(string path, string expectedWord)
    {
        var res = await _client.GetAsync(path);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains(expectedWord, body, StringComparison.OrdinalIgnoreCase);
    }

    // ── S1-08 404 ──────────────────────────────────────────────────────
    [Fact]
    public async Task S1_08_UnknownRoute_Returns404()
    {
        var res = await _client.GetAsync("/this-path-does-not-exist-" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ── S1-09 500 page ─────────────────────────────────────────────────
    // Note: requires a dev-only /debug/throw endpoint or equivalent. This test
    // asserts the error page route itself is reachable.
    [Fact]
    public async Task S1_09_ErrorPage_IsReachable()
    {
        var res = await _client.GetAsync("/Error/500");
        // 500 page may return 500 status but must render an Error body.
        Assert.True(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.InternalServerError);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Error", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── S1-10 admin login renders ──────────────────────────────────────
    [Fact]
    public async Task S1_10_AdminLogin_Renders()
    {
        var res = await _client.GetAsync("/Admin/Login");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("email", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("password", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── S1-11 valid login → dashboard ──────────────────────────────────
    [Fact]
    public async Task S1_11_AdminLogin_WithValidCreds_RedirectsToDashboard()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Fetch antiforgery token
        var loginPage = await client.GetAsync("/Admin/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        var cookies = loginPage.Headers.GetValues("Set-Cookie");
        var cookieHeader = string.Join("; ", cookies.Select(c => c.Split(';')[0]));

        using var req = new HttpRequestMessage(HttpMethod.Post, "/Admin/Login");
        req.Headers.Add("Cookie", cookieHeader);
        req.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", SeededAdmin.Email),
            new KeyValuePair<string, string>("Password", SeededAdmin.Password),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Admin/Dashboard", res.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ── S1-12 invalid creds → error ────────────────────────────────────
    [Fact]
    public async Task S1_12_AdminLogin_WithInvalidCreds_ShowsError()
    {
        var client = _factory.CreateClient();
        var loginPage = await client.GetAsync("/Admin/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        var cookies = loginPage.Headers.GetValues("Set-Cookie");
        var cookieHeader = string.Join("; ", cookies.Select(c => c.Split(';')[0]));

        using var req = new HttpRequestMessage(HttpMethod.Post, "/Admin/Login");
        req.Headers.Add("Cookie", cookieHeader);
        req.Content = new FormUrlEncodedContent(new[]
        {
            // Use a non-existent email so failed attempts don't lock the real admin
            // (the shared fixture's admin must stay usable for S1-11, S1-15, S1-16).
            new KeyValuePair<string, string>("Email", "nobody-" + Guid.NewGuid() + "@example.test"),
            new KeyValuePair<string, string>("Password", "wrong-password-" + Guid.NewGuid()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        var res = await client.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("invalid", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── S1-13 lockout after 5 failed attempts ──────────────────────────
    [Fact(Skip = "Slow; requires 5 sequential logins. Covered manually + in Slice 1 CI smoke.")]
    public Task S1_13_AdminLockout_After5FailedAttempts() => Task.CompletedTask;

    // ── S1-14 /admin/* auth gate ───────────────────────────────────────
    [Theory]
    [InlineData("/Admin/Dashboard")]
    [InlineData("/Admin/Settings")]
    public async Task S1_14_AdminRoutes_RedirectToLogin_WhenUnauthenticated(string path)
    {
        var res = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ── S1-15 dashboard metric cards ───────────────────────────────────
    [Fact]
    public async Task S1_15_AdminDashboard_ShowsMetricCards()
    {
        var client = await LoggedInClient();
        var res = await client.GetAsync("/Admin/Dashboard");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        // Dashboard has metric cards with labels
        Assert.Contains("Page Views", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Submission", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── S1-16 site settings edit → saved to DB ─────────────────────────
    [Fact]
    public async Task S1_16_AdminSiteSettings_Save_PersistsToDb()
    {
        var client = await LoggedInClient();

        // Fetch settings page for antiforgery token
        var page = await client.GetAsync("/Admin/Settings");
        page.EnsureSuccessStatusCode();
        var html = await page.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var newPhone = "(555) 777-" + Random.Shared.Next(1000, 9999).ToString();
        using var req = new HttpRequestMessage(HttpMethod.Post, "/Admin/Settings");
        req.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Input.PhoneNumber", newPhone),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        var res = await client.SendAsync(req);
        Assert.True(res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.Redirect);

        // Verify persisted
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = db.Set<SiteSettings>().FirstOrDefault();
        Assert.NotNull(settings);
        Assert.Equal(newPhone, settings!.PhoneNumber);
    }

    // ── S1-17 footer reflects SiteSettings ─────────────────────────────
    [Fact]
    public async Task S1_17_Footer_RendersPhoneFromSiteSettings()
    {
        // Seed a known phone
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var s = db.Set<SiteSettings>().FirstOrDefault() ?? new SiteSettings { SiteName = "The Switchboard" };
            s.PhoneNumber = "(800) 555-0111";
            if (s.Id == 0) { db.Add(s); } else { db.Update(s); }
            await db.SaveChangesAsync();
        }
        var body = await _client.GetStringAsync("/");
        Assert.Contains("(800) 555-0111", body);
    }

    // ── S1-18 no "lead/leads" in customer-facing copy ──────────────────
    [Fact]
    public async Task S1_18_PublicCopy_HasNoLeadLanguage()
    {
        var paths = new[] { "/", "/privacy", "/terms", "/accessibility" };
        var bannedWords = new[] { "\\blead\\b", "\\bleads\\b" };
        foreach (var path in paths)
        {
            var body = await _client.GetStringAsync(path);
            // Strip HTML tags to avoid false positives on attribute names.
            var text = Regex.Replace(body, "<[^>]+>", " ");
            foreach (var w in bannedWords)
            {
                Assert.False(Regex.IsMatch(text, w, RegexOptions.IgnoreCase),
                    $"Customer-facing text on {path} contains banned word matching /{w}/i");
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Matches the admin seed defaults in appsettings.Development.json.
    /// If the seed configuration differs, these constants must be updated.
    /// </summary>
    private static class SeededAdmin
    {
        public const string Email = "admin@theswitchboard.local";
        public const string Password = "SwitchboardDev2026!";
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html,
            @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private async Task<HttpClient> LoggedInClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Admin/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        var cookieHeader = string.Join("; ",
            loginPage.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]));

        using var req = new HttpRequestMessage(HttpMethod.Post, "/Admin/Login");
        req.Headers.Add("Cookie", cookieHeader);
        req.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", SeededAdmin.Email),
            new KeyValuePair<string, string>("Password", SeededAdmin.Password),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        var res = await client.SendAsync(req);
        if (res.StatusCode != HttpStatusCode.Redirect)
            throw new InvalidOperationException("Login did not redirect — test fixture cannot authenticate.");

        var authCookies = res.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]);
        var authed = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        authed.DefaultRequestHeaders.Add("Cookie", string.Join("; ", authCookies));
        return authed;
    }
}
