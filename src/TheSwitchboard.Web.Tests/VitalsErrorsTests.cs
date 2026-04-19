using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice T-6 — Web Vitals + JS errors + ErrorImpact.
///
///   T6_01 — An unhandled error payload persists ONE JsError row with a 16-char
///           fingerprint + redacted stack. Duplicate post increments Count, no new row.
///   T6_02 — LCP vital at 2400ms → rating="good"; at 4500ms → rating="poor".
///   T6_03 — ErrorImpactService returns positive CVR-delta for a session that saw
///           an error and did NOT convert vs sessions that did convert without it.
///   T6_04 — /vitals and /errors reject foreign origin with 403.
///   T6_05..07 — /Admin/Reports/Performance, /Errors, /ErrorImpact anon → 302 → login.
/// </summary>
public class VitalsErrorsTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public VitalsErrorsTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private async Task<HttpResponseMessage> Post(string path, object body, string origin = "http://localhost")
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Add("Origin", origin);
        req.Content = Json(body);
        return await _client.SendAsync(req);
    }

    // ── T6_01 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T6_01_UnhandledError_PersistsOneRow_DedupedByFingerprint()
    {
        var sid = "t6err_" + Guid.NewGuid().ToString("N")[..12];
        var payload = new
        {
            errors = new[]
            {
                new
                {
                    sid,
                    vid = "v",
                    path = "/",
                    ts = DateTime.UtcNow,
                    message = "TypeError: Cannot read properties of null (reading 'focus')",
                    stack = "at HTMLButtonElement.<anonymous> (app.js:42:15)\n    at dispatch (tracker.js:12:3)",
                    source = "https://www.example.com/app.js",
                    line = 42,
                    col = 15,
                    userAgent = "Mozilla/5.0",
                    buildId = "abc123"
                }
            }
        };
        (await Post("/api/tracking/errors", payload)).EnsureSuccessStatusCode();
        (await Post("/api/tracking/errors", payload)).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.Set<JsError>().Where(e => e.SessionId == sid).ToListAsync();
        Assert.Single(rows);
        Assert.Equal(2, rows[0].Count);
        Assert.False(string.IsNullOrEmpty(rows[0].Fingerprint));
        Assert.Equal(16, rows[0].Fingerprint!.Length);
    }

    // ── T6_02 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T6_02_LCP_RatingComputed()
    {
        var sid = "t6lcp_" + Guid.NewGuid().ToString("N")[..12];
        var ts = DateTime.UtcNow;
        var payload = new
        {
            vitals = new[]
            {
                new { sid, vid = "v", path = "/", ts, metric = "LCP", value = 2400.0, navigationType = "navigate" },
                new { sid, vid = "v", path = "/", ts, metric = "LCP", value = 4500.0, navigationType = "navigate" }
            }
        };
        (await Post("/api/tracking/vitals", payload)).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.Set<WebVitalSample>().Where(v => v.SessionId == sid).OrderBy(v => v.Value).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal("good", rows[0].Rating);
        Assert.Equal("poor", rows[1].Rating);
    }

    // ── T6_03 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T6_03_ErrorImpact_ComputesCvrDelta()
    {
        // Seed sessions directly so the Session.Converted flag is deterministic.
        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            var fp = "fptest_abcd1234";

            // 2 sessions saw the error: one converted, one didn't (CVR=50%).
            // 2 sessions did NOT see the error, both converted (CVR=100%).
            db.Sessions.Add(new Session { Id = "t6cvr_a", StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow, Converted = true,  IsBot = false });
            db.Sessions.Add(new Session { Id = "t6cvr_b", StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow, Converted = false, IsBot = false });
            db.Sessions.Add(new Session { Id = "t6cvr_c", StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow, Converted = true,  IsBot = false });
            db.Sessions.Add(new Session { Id = "t6cvr_d", StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow, Converted = true,  IsBot = false });
            db.Set<JsError>().Add(new JsError { SessionId = "t6cvr_a", Path = "/", Ts = DateTime.UtcNow, Message = "err", Fingerprint = fp, Count = 1 });
            db.Set<JsError>().Add(new JsError { SessionId = "t6cvr_b", Path = "/", Ts = DateTime.UtcNow, Message = "err", Fingerprint = fp, Count = 1 });
            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IErrorImpactService>();
        var rows = await svc.ComputeAsync(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        var ours = rows.FirstOrDefault(r => r.Fingerprint == "fptest_abcd1234");
        Assert.NotNull(ours);
        Assert.Equal(2, ours!.SessionsAffected);
        Assert.Equal(50, ours.CvrAffectedPct);
        Assert.True(ours.CvrDeltaPct < 0, "CVR delta should be negative when error hurts conversion");
    }

    // ── T6_04 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T6_04_Vitals_And_Errors_RejectForeignOrigin()
    {
        var v = await Post("/api/tracking/vitals", new { vitals = Array.Empty<object>() }, "https://evil.example.com");
        var e = await Post("/api/tracking/errors", new { errors = Array.Empty<object>() }, "https://evil.example.com");
        Assert.Equal(HttpStatusCode.Forbidden, v.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, e.StatusCode);
    }

    // ── T6_05..07 ──────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Admin/Reports/Performance")]
    [InlineData("/Admin/Reports/Errors")]
    [InlineData("/Admin/Reports/ErrorImpact")]
    public async Task T6_AdminPages_Anonymous_RedirectsToLogin(string path)
    {
        var res = await _client.GetAsync(path);
        Assert.True(
            res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"{path}: expected redirect, got {(int)res.StatusCode}");
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
