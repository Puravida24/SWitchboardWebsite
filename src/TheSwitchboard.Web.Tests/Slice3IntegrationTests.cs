using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Content;
using TheSwitchboard.Web.Models.Site;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice 3 BDD scenarios (S3-01 through S3-20).
/// RED phase — tests should fail until Slice 3 implementation lands.
/// </summary>
public class Slice3IntegrationTests : IClassFixture<Slice3Factory>
{
    private readonly Slice3Factory _factory;
    private readonly HttpClient _client;

    public Slice3IntegrationTests(Slice3Factory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ── S3-01 homepage renders from DB, not hardcoded strings ──────────
    [Fact]
    public async Task S3_01_HomepageCopy_ComesFromDb()
    {
        await UpdateSiteSettings(s =>
        {
            s.HeroHeadline = "CUSTOM-HERO-HEADLINE-SLICE3";
            s.HeroDeck = "Custom deck for tests.";
        });
        var body = await _client.GetStringAsync("/");
        Assert.Contains("CUSTOM-HERO-HEADLINE-SLICE3", body);
        Assert.Contains("Custom deck for tests.", body);
    }

    // ── S3-02 admin edit → public updates ──────────────────────────────
    [Fact]
    public async Task S3_02_AdminEditHeroHeadline_PublicPageReflects()
    {
        var authed = await _factory.LoggedInClientAsync();
        var marker = "MARKER-" + Guid.NewGuid().ToString("N")[..8];
        await PostAdminSettings(authed, new() { ["Input.HeroHeadline"] = marker });
        var body = await _client.GetStringAsync("/");
        Assert.Contains(marker, body);
    }

    // ── S3-03 roster admin page renders + has create form ──────────────
    [Fact]
    public async Task S3_03_AdminPartnersPage_Renders()
    {
        await SeedPartner("AcmeCarrier", active: true, order: 0);
        var authed = await _factory.LoggedInClientAsync();
        var res = await authed.GetAsync("/Admin/Partners");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("AcmeCarrier", body);
        Assert.Contains("Add partner", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── S3-04 active roster logos appear in ecosystem carousel ─────────
    [Fact]
    public async Task S3_04_ActivePartners_ShowInCarousel()
    {
        await SeedPartner("VisiblePartner", active: true, order: 1);
        var body = await _client.GetStringAsync("/");
        Assert.Contains("VisiblePartner", body);
    }

    // ── S3-05 inactive partner hidden from public ──────────────────────
    [Fact]
    public async Task S3_05_InactivePartner_Hidden()
    {
        await SeedPartner("HiddenPartner", active: false, order: 2);
        var body = await _client.GetStringAsync("/");
        Assert.DoesNotContain("HiddenPartner", body);
    }

    // ── S3-09/10/11 legal pages from DB ────────────────────────────────
    [Theory]
    [InlineData("privacy", "/privacy")]
    [InlineData("terms", "/terms")]
    [InlineData("accessibility", "/accessibility")]
    public async Task S3_09_10_11_LegalPages_RenderFromDb(string slug, string path)
    {
        var marker = $"LEGAL-MARKER-{slug}-{Guid.NewGuid().ToString("N")[..6]}";
        await SeedLegalPage(slug, $"<p>{marker}</p>");
        var body = await _client.GetStringAsync(path);
        Assert.Contains(marker, body);
    }

    // ── S3-12 version history shows last 10 per field ──────────────────
    [Fact]
    public async Task S3_12_VersionHistory_RecordsLastEdits()
    {
        var authed = await _factory.LoggedInClientAsync();
        await PostAdminSettings(authed, new() { ["Input.HeroHeadline"] = "v1" });
        await PostAdminSettings(authed, new() { ["Input.HeroHeadline"] = "v2" });
        var res = await authed.GetAsync("/Admin/History?type=sitesettings&field=HeroHeadline");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("v1", body);
        Assert.Contains("v2", body);
    }

    // ── S3-16 empty required field → validation error ──────────────────
    [Fact]
    public async Task S3_16_EmptyRequiredField_ShowsValidationError()
    {
        var authed = await _factory.LoggedInClientAsync();
        var res = await PostAdminSettingsRaw(authed, new() { ["Input.SiteName"] = "" });
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("required", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── S3-17 XSS in rich text sanitized ───────────────────────────────
    [Fact]
    public async Task S3_17_LegalPage_XssSanitized()
    {
        await SeedLegalPage("privacy", "<p>ok</p><script>alert(1)</script><img src=x onerror=alert(1)>");
        var body = await _client.GetStringAsync("/privacy");
        Assert.DoesNotContain("<script>", body);
        Assert.DoesNotContain("onerror=", body);
    }


    // ── S3-19 unauthenticated /admin/content → redirect to login ───────
    [Fact]
    public async Task S3_19_AdminContent_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Partners");
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ── S3-20 save blocked if text contains "lead/leads" ───────────────
    [Fact]
    public async Task S3_20_SaveWithLeadLanguage_Rejected()
    {
        var authed = await _factory.LoggedInClientAsync();
        var res = await PostAdminSettingsRaw(authed, new() { ["Input.HeroHeadline"] = "Get quality leads fast" });
        var body = await res.Content.ReadAsStringAsync();
        // Either 400 / validation error surfaced, or the save silently dropped.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await db.Set<SiteSettings>().FirstAsync();
        Assert.DoesNotContain("lead", settings.HeroHeadline ?? "", StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private async Task UpdateSiteSettings(Action<SiteSettings> mutate)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var s = await db.Set<SiteSettings>().FirstOrDefaultAsync()
            ?? new SiteSettings { SiteName = "The Switchboard" };
        mutate(s);
        if (s.Id == 0) db.Add(s); else db.Update(s);
        await db.SaveChangesAsync();
    }

    private async Task SeedPartner(string name, bool active, int order)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Add(new ClientLogo
        {
            CompanyName = name,
            LogoUrl = "/uploads/partners/test.png",
            SortOrder = order,
            IsActive = active
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedLegalPage(string slug, string html)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.Set<LegalPage>().FirstOrDefaultAsync(p => p.Slug == slug);
        if (existing is null)
        {
            db.Add(new LegalPage { Slug = slug, HtmlContent = html });
        }
        else
        {
            existing.HtmlContent = html;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    private async Task<HttpResponseMessage> PostAdminSettingsRaw(HttpClient authed, Dictionary<string, string> fields)
    {
        var getPage = await authed.GetAsync("/Admin/Settings");
        var html = await getPage.Content.ReadAsStringAsync();
        var token = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        // Ensure SiteName always present (required)
        if (!fields.ContainsKey("Input.SiteName")) fields["Input.SiteName"] = "The Switchboard";
        fields["__RequestVerificationToken"] = token;
        var content = new FormUrlEncodedContent(fields);
        return await authed.PostAsync("/Admin/Settings", content);
    }

    private async Task PostAdminSettings(HttpClient authed, Dictionary<string, string> fields)
    {
        var res = await PostAdminSettingsRaw(authed, fields);
        Assert.True(res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.Redirect,
            $"Admin settings save failed: {(int)res.StatusCode}");
    }
}
