using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice T-7C — Compliance admin + Phoenix match API.
///
///   T7C_01 — ComplianceAnalyticsService computes capture-rate =
///            (certified submissions) / (total submissions) × 100.
///   T7C_02..05 — /Admin/Reports/Compliance, /Certificates, /Certificates/Detail,
///            /Disclosures all redirect anon to /Admin/Login.
///   T7C_06 — POST /api/consent/match with missing bearer → 401.
///   T7C_07 — Valid bearer + matching email + phone hashes → match=true,
///            matchedFields contains both.
///   T7C_08 — Valid bearer + non-matching email/phone → match=false.
///   T7C_09 — Valid bearer but cert is expired → 410.
///   T7C_10 — Valid bearer + unknown certificateId → 404.
/// </summary>
public class ComplianceMatchTests : IClassFixture<ComplianceMatchTests.KeyedFactory>
{
    private readonly KeyedFactory _factory;
    private readonly HttpClient _client;
    private const string Key = "test-phoenix-consent-key-xyz";

    public sealed class KeyedFactory : SwitchboardWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((ctx, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PhoenixCrm:ConsentApiKey"] = Key
                });
            });
        }
    }

    public ComplianceMatchTests(KeyedFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<(string certId, ConsentCertificate cert)> SeedCertAsync(
        string email, string phone, DateTime? expiresAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = "sw_cert_" + Guid.NewGuid().ToString("N")[..16];
        var cert = new ConsentCertificate
        {
            CertificateId = id,
            SessionId = "s_" + Guid.NewGuid().ToString("N")[..8],
            ConsentTimestamp = DateTime.UtcNow.AddMinutes(-5),
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddYears(5),
            DisclosureText = "By submitting you consent.",
            DisclosureTextHash = Sha256Hex("By submitting you consent."),
            DisclosureIsVisible = true,
            EmailHash = Sha256Hex(email),
            PhoneHash = Sha256Hex(phone)
        };
        db.ConsentCertificates.Add(cert);
        await db.SaveChangesAsync();
        return (id, cert);
    }

    private HttpRequestMessage BuildMatch(string json, string? key = Key)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/consent/match");
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        if (key is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        return req;
    }

    // ── T7C_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7C_01_CaptureRate_Computed()
    {
        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            // 4 submissions — 3 with a cert link, 1 without.
            for (var i = 0; i < 4; i++)
            {
                var sub = new TheSwitchboard.Web.Models.Forms.FormSubmission
                {
                    FormType = "contact",
                    Data = "{}",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                };
                db.FormSubmissions.Add(sub);
                await db.SaveChangesAsync();
                if (i < 3)
                {
                    var (_, cert) = await SeedCertAsync($"a{i}@x.com", $"555000000{i}");
                    cert.FormSubmissionId = sub.Id;
                    sub.ConsentCertificateId = cert.Id;
                    await db.SaveChangesAsync();
                }
            }
        }

        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IComplianceAnalyticsService>();
        var report = await svc.GetAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        Assert.True(report.TotalSubmissions >= 4);
        Assert.True(report.CertifiedSubmissions >= 3);
        Assert.InRange(report.CaptureRatePct, 70, 100);
    }

    // ── T7C_02..05 ─────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Admin/Reports/Compliance")]
    [InlineData("/Admin/Reports/Certificates")]
    [InlineData("/Admin/Reports/Certificates/Detail?id=x")]
    [InlineData("/Admin/Reports/Disclosures")]
    public async Task T7C_AdminPages_Anonymous_RedirectsToLogin(string path)
    {
        var res = await _client.GetAsync(path);
        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"{path}: expected redirect, got {(int)res.StatusCode}");
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ── T7C_06 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7C_06_Match_MissingBearer_Returns401()
    {
        var req = BuildMatch("{}", key: null);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── T7C_07 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7C_07_Match_Valid_ReturnsMatchTrue()
    {
        var email = "alpha@example.com";
        var phone = "+15555550123";
        var (certId, _) = await SeedCertAsync(email, phone);

        var body = JsonSerializer.Serialize(new { certificateId = certId, email, phone });
        var res = await _client.SendAsync(BuildMatch(body));
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("match").GetBoolean());
        var fields = doc.RootElement.GetProperty("matchedFields").EnumerateArray()
            .Select(x => x.GetString()).ToArray();
        Assert.Contains("email", fields);
        Assert.Contains("phone", fields);
    }

    // ── T7C_08 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7C_08_Match_NonMatching_ReturnsMatchFalse()
    {
        var (certId, _) = await SeedCertAsync("bravo@example.com", "+15555550199");
        var body = JsonSerializer.Serialize(new { certificateId = certId, email = "other@example.com", phone = "+15555550000" });
        var res = await _client.SendAsync(BuildMatch(body));
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("match").GetBoolean());
        Assert.Empty(doc.RootElement.GetProperty("matchedFields").EnumerateArray());
    }

    // ── T7C_09 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7C_09_Match_ExpiredCert_Returns410()
    {
        var (certId, _) = await SeedCertAsync("charlie@example.com", "+15555550077",
            expiresAt: DateTime.UtcNow.AddDays(-1));
        var body = JsonSerializer.Serialize(new { certificateId = certId, email = "charlie@example.com", phone = "+15555550077" });
        var res = await _client.SendAsync(BuildMatch(body));
        Assert.Equal(HttpStatusCode.Gone, res.StatusCode);
    }

    // ── T7C_10 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T7C_10_Match_UnknownCert_Returns404()
    {
        var body = JsonSerializer.Serialize(new { certificateId = "sw_cert_nope_nope_nope", email = "x@y.com", phone = "1" });
        var res = await _client.SendAsync(BuildMatch(body));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
