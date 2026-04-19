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
/// Slice T-7B — TCPA Consent Certificate foundation.
///
///   T7B_01 — POST /api/tracking/consent creates a ConsentCertificate row
///            with a "sw_cert_…" id and 5-year ExpiresAt.
///   T7B_02 — First POST with a never-seen disclosure text hash auto-creates
///            a DisclosureVersion row with Status="auto-detected".
///   T7B_03 — /verify/{id} public page returns 200 and does NOT leak IP /
///            EmailHash / PhoneHash in the body.
///   T7B_04 — Cert whose ExpiresAt is in the past returns 410 from /verify/{id}.
///   T7B_05 — Minimal behavioral signals (0 keystrokes, 0 mouse distance) →
///            IsSuspiciousBot=true on the persisted cert row.
///   T7B_06 — Foreign Origin rejected with 403.
/// </summary>
public class ConsentCertificateTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ConsentCertificateTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private async Task<HttpResponseMessage> Post(object body, string origin = "http://localhost")
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/consent");
        req.Headers.Add("Origin", origin);
        req.Content = Json(body);
        return await _client.SendAsync(req);
    }

    private static object Proof(string disclosureText, int keystrokes = 40, int mouseDist = 4200, int scrollPct = 62)
    {
        return new
        {
            sid = "t7b_" + Guid.NewGuid().ToString("N")[..12],
            vid = "v_" + Guid.NewGuid().ToString("N")[..10],
            consentTimestamp = DateTime.UtcNow,
            consentMethod = "submit-button",
            consentElementSelector = "button[type=submit]",
            clickX = 420,
            clickY = 860,
            pageLoadedAt = DateTime.UtcNow.AddSeconds(-45),
            timeOnPageSeconds = 45,
            disclosureText,
            disclosureFontSize = "13px",
            disclosureColor = "#1f2937",
            disclosureBackgroundColor = "#ffffff",
            disclosureContrastRatio = 14.2,
            disclosureIsVisible = true,
            userAgent = "Mozilla/5.0 (Macintosh) Safari/605",
            browserName = "Safari",
            osName = "macOS",
            screenResolution = "2880x1800",
            viewportW = 1440,
            viewportH = 900,
            pageUrl = "https://www.theswitchboardmarketing.com/",
            keystrokesPerMinute = keystrokes,
            formFieldsInteracted = 6,
            mouseDistancePx = mouseDist,
            scrollDepthPercent = scrollPct,
            emailHashHex = "abc123".PadRight(64, '0'),
            phoneHashHex = "def456".PadRight(64, '0')
        };
    }

    // ── T7B_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7B_01_Consent_CreatesCertificate_WithSwCertId_And5YearExpiry()
    {
        var res = await Post(Proof("By submitting, you consent to receive calls about your inquiry."));
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var certId = doc.RootElement.GetProperty("certificateId").GetString();
        Assert.NotNull(certId);
        Assert.StartsWith("sw_cert_", certId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Set<ConsentCertificate>().FirstOrDefaultAsync(c => c.CertificateId == certId);
        Assert.NotNull(row);
        var delta = row!.ExpiresAt - row.CreatedAt;
        Assert.InRange(delta.TotalDays, 5 * 365 - 2, 5 * 365 + 2);
    }

    // ── T7B_02 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7B_02_UnknownDisclosureHash_AutoCreatesDisclosureVersion()
    {
        var unique = "By submitting " + Guid.NewGuid().ToString("N") + " you consent to calls.";
        var res = await Post(Proof(unique));
        res.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var versions = await db.Set<DisclosureVersion>().Where(v => v.FullText == unique).ToListAsync();
        Assert.Single(versions);
        Assert.Equal("auto-detected", versions[0].Status);
    }

    // ── T7B_03 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7B_03_VerifyPublicPage_HidesPii()
    {
        var text = "You consent to calls — T7B_03 variant.";
        var res = await Post(Proof(text));
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var certId = doc.RootElement.GetProperty("certificateId").GetString();

        var verify = await _client.GetAsync($"/verify/{certId}");
        verify.EnsureSuccessStatusCode();
        var html = await verify.Content.ReadAsStringAsync();

        // Disclosure text should appear.
        Assert.Contains("T7B_03 variant", html);
        // PII must NOT leak.
        Assert.DoesNotContain("abc123000", html); // email hash prefix
        Assert.DoesNotContain("def456000", html); // phone hash prefix
        Assert.DoesNotContain("RemoteIpAddress", html);
    }

    // ── T7B_04 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7B_04_ExpiredCert_Returns410()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cert = new ConsentCertificate
        {
            CertificateId = "sw_cert_expired_" + Guid.NewGuid().ToString("N")[..8],
            SessionId = "sid_x",
            ConsentTimestamp = DateTime.UtcNow.AddYears(-6),
            CreatedAt = DateTime.UtcNow.AddYears(-6),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            DisclosureText = "old text",
            DisclosureTextHash = "expired_hash_0000",
            PageUrl = "/",
            UserAgent = "Mozilla/5.0",
            BrowserName = "Safari", OsName = "macOS"
        };
        db.Set<ConsentCertificate>().Add(cert);
        await db.SaveChangesAsync();

        var verify = await _client.GetAsync($"/verify/{cert.CertificateId}");
        Assert.Equal(HttpStatusCode.Gone, verify.StatusCode);
    }

    // ── T7B_05 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7B_05_ZeroBehavioralSignals_FlagsSuspiciousBot()
    {
        var res = await Post(Proof("Bot consent", keystrokes: 0, mouseDist: 0, scrollPct: 0));
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var certId = doc.RootElement.GetProperty("certificateId").GetString();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Set<ConsentCertificate>().FirstAsync(c => c.CertificateId == certId);
        Assert.True(row.IsSuspiciousBot);
    }

    // ── T7B_06 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7B_06_ForeignOrigin_Returns403()
    {
        var res = await Post(Proof("x"), origin: "https://evil.example.com");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
