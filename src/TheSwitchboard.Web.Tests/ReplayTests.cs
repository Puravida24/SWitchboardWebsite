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
/// Slice T-7 — Session replay (rrweb).
///
///   T7_01 — First chunk POST creates one Replay envelope + one ReplayChunk row
///           with sequence=0. ByteSize populated.
///   T7_02 — Second chunk on the same sid appends (ChunkCount=2, Sequence=1).
///   T7_03 — Oversized chunk (&gt;512 KB) is rejected (413 or silently dropped).
///   T7_04 — replay.js is served and references maskAllInputs +
///           /api/tracking/replay/chunk + sample gate.
///   T7_05 — rrweb-record.min.js is served under /js/vendor/.
///   T7_06 — Foreign Origin → 403 on the chunk endpoint.
///   T7_07 — /Admin/Reports/Sessions/Detail anon → 302 → /Admin/Login.
/// </summary>
public class ReplayTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReplayTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private async Task<HttpResponseMessage> PostChunk(object body, string origin = "http://localhost")
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tracking/replay/chunk");
        req.Headers.Add("Origin", origin);
        req.Content = Json(body);
        return await _client.SendAsync(req);
    }

    // ── T7_01 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T7_01_FirstChunk_CreatesReplayAndChunk()
    {
        var sid = "t7_" + Guid.NewGuid().ToString("N")[..12];
        var bytes = new byte[1024];
        Random.Shared.NextBytes(bytes);
        var b64 = Convert.ToBase64String(bytes);

        var res = await PostChunk(new
        {
            sid,
            sequence = 0,
            ts = DateTime.UtcNow,
            compressed = true,
            payloadBase64 = b64
        });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var replay = await db.Set<Replay>().FirstOrDefaultAsync(r => r.SessionId == sid);
        Assert.NotNull(replay);
        Assert.Equal(1, replay!.ChunkCount);
        Assert.Equal(1024, replay.ByteSize);
        Assert.True(replay.Compressed);

        var chunks = await db.Set<ReplayChunk>().Where(c => c.ReplayId == replay.Id).ToListAsync();
        Assert.Single(chunks);
        Assert.Equal(0, chunks[0].Sequence);
        Assert.Equal(1024, chunks[0].Payload.Length);
    }

    // ── T7_02 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T7_02_SecondChunkSameSid_Appends()
    {
        var sid = "t7_" + Guid.NewGuid().ToString("N")[..12];
        var b64 = Convert.ToBase64String(new byte[512]);
        (await PostChunk(new { sid, sequence = 0, ts = DateTime.UtcNow, compressed = true, payloadBase64 = b64 })).EnsureSuccessStatusCode();
        (await PostChunk(new { sid, sequence = 1, ts = DateTime.UtcNow, compressed = true, payloadBase64 = b64 })).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var replay = await db.Set<Replay>().FirstAsync(r => r.SessionId == sid);
        Assert.Equal(2, replay.ChunkCount);
        Assert.Equal(1024, replay.ByteSize);
        var chunks = await db.Set<ReplayChunk>()
            .Where(c => c.ReplayId == replay.Id)
            .OrderBy(c => c.Sequence)
            .ToListAsync();
        Assert.Equal(new[] { 0, 1 }, chunks.Select(c => c.Sequence).ToArray());
    }

    // ── T7_03 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T7_03_OversizedChunk_Rejected()
    {
        var sid = "t7big_" + Guid.NewGuid().ToString("N")[..8];
        // 600 KB > 512 KB cap.
        var bytes = new byte[600 * 1024];
        var b64 = Convert.ToBase64String(bytes);

        var res = await PostChunk(new { sid, sequence = 0, ts = DateTime.UtcNow, compressed = true, payloadBase64 = b64 });
        Assert.True(
            res.StatusCode is HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.NoContent,
            $"Expected 413 or 204 (silent drop), got {(int)res.StatusCode}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var replay = await db.Set<Replay>().FirstOrDefaultAsync(r => r.SessionId == sid);
        Assert.Null(replay);
    }

    // ── T7_04 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T7_04_ReplayJs_IsServedWithMaskConfig()
    {
        var res = await _client.GetAsync("/js/tracker/replay.js");
        res.EnsureSuccessStatusCode();
        var js = await res.Content.ReadAsStringAsync();
        Assert.Contains("/api/tracking/replay/chunk", js);
        Assert.Contains("maskAllInputs", js);
        Assert.Contains("data-tb-pii", js);
    }

    // ── T7_05 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T7_05_RrwebRecord_IsServedFromVendor()
    {
        var res = await _client.GetAsync("/js/vendor/rrweb-record.min.js");
        Assert.True(res.IsSuccessStatusCode, $"rrweb-record.min.js must be vendored under /js/vendor/ (got {(int)res.StatusCode})");
    }

    // ── T7_06 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T7_06_ChunkEndpoint_ForeignOrigin_Returns403()
    {
        var res = await PostChunk(new { sid = "x", sequence = 0, payloadBase64 = "AAAA" }, origin: "https://evil.example.com");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── T7_07 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T7_07_SessionsDetail_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/Sessions/Detail?id=anything");
        Assert.True(
            res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"Expected redirect, got {(int)res.StatusCode}");
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
