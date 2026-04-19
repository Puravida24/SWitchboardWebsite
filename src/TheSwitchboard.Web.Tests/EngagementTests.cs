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
/// Slice T-5 — Scroll + Mouse trail + Form funnel.
///
///   T5_01 — Form field abandon event persists (abandoned field name kept).
///   T5_02 — Same (sid, path, depth) POSTed twice creates ONE ScrollSample row.
///   T5_03 — Mouse trail capped at 300 rows per session (301st dropped).
///   T5_04 — scroll.js / mousetrail.js / forms.js served with expected hooks.
///   T5_05 — /api/tracking/scroll / /mouse-trail / /form-events reject foreign Origin.
///   T5_06 — /Admin/Reports/Forms/Funnel redirects anon.
///   T5_07 — /Admin/Reports/Abandonment redirects anon.
///   T5_08 — /Admin/Reports/Heatmaps/Scroll redirects anon.
/// </summary>
public class EngagementTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EngagementTests(SwitchboardWebApplicationFactory factory)
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

    private async Task<HttpResponseMessage> Post(string path, object body, string origin = "http://localhost")
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Add("Origin", origin);
        req.Content = Json(body);
        return await _client.SendAsync(req);
    }

    // ── T5_01 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T5_01_FormAbandonEvent_Persists()
    {
        var sid = "t5_form_" + Guid.NewGuid().ToString("N")[..12];
        var payload = new
        {
            events = new[]
            {
                new
                {
                    sid,
                    vid = "v",
                    path = "/",
                    formId = "contact",
                    fieldName = "message",
                    @event = "abandon",
                    occurredAt = DateTime.UtcNow,
                    dwellMs = 7500,
                    charCount = 42,
                    correctionCount = 3,
                    pastedFlag = false
                }
            }
        };
        var res = await Post("/api/tracking/form-events", payload);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Set<FormInteraction>().FirstOrDefaultAsync(f => f.SessionId == sid);
        Assert.NotNull(row);
        Assert.Equal("contact", row!.FormId);
        Assert.Equal("message", row.FieldName);
        Assert.Equal("abandon", row.Event);
        Assert.Equal(7500, row.DwellMs);
    }

    // ── T5_02 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T5_02_ScrollMilestones_DedupePerSessionPath()
    {
        var sid = "t5_scroll_" + Guid.NewGuid().ToString("N")[..12];
        var milestone = new
        {
            sid,
            vid = "v",
            path = "/",
            ts = DateTime.UtcNow,
            depth = 50,
            maxDepth = 50,
            viewportH = 900,
            documentH = 3000,
            timeSinceLoadMs = 4200
        };
        var res1 = await Post("/api/tracking/scroll", new { samples = new[] { milestone } });
        var res2 = await Post("/api/tracking/scroll", new { samples = new[] { milestone } });
        Assert.Equal(HttpStatusCode.NoContent, res1.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, res2.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Set<ScrollSample>().CountAsync(s => s.SessionId == sid && s.Depth == 50);
        Assert.Equal(1, count);
    }

    // ── T5_03 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T5_03_MouseTrail_CappedAt300PerSession()
    {
        var sid = "t5_mouse_" + Guid.NewGuid().ToString("N")[..12];
        var baseTs = DateTime.UtcNow;

        async Task Batch(int from, int count)
        {
            var rows = Enumerable.Range(from, count).Select(i => new
            {
                sid,
                vid = "v",
                path = "/",
                ts = baseTs.AddMilliseconds(i * 200),
                x = i,
                y = i,
                viewportW = 1920,
                viewportH = 1080
            }).ToArray();
            (await Post("/api/tracking/mouse-trail", new { points = rows })).EnsureSuccessStatusCode();
        }

        await Batch(0, 200);
        await Batch(200, 200); // total 400, cap 300

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Set<MouseTrail>().CountAsync(m => m.SessionId == sid);
        Assert.Equal(300, count);
    }

    // ── T5_04 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T5_04_ThreeClientJsModulesServed()
    {
        var scroll = await _client.GetAsync("/js/tracker/scroll.js");
        var mouse  = await _client.GetAsync("/js/tracker/mousetrail.js");
        var forms  = await _client.GetAsync("/js/tracker/forms.js");
        scroll.EnsureSuccessStatusCode();
        mouse.EnsureSuccessStatusCode();
        forms.EnsureSuccessStatusCode();

        var scrollJs = await scroll.Content.ReadAsStringAsync();
        var mouseJs  = await mouse.Content.ReadAsStringAsync();
        var formsJs  = await forms.Content.ReadAsStringAsync();

        Assert.Contains("/api/tracking/scroll", scrollJs);
        Assert.Contains("/api/tracking/mouse-trail", mouseJs);
        Assert.Contains("/api/tracking/form-events", formsJs);
        Assert.Contains("data-tb-field", formsJs);
        // Milestones constants should be explicit in scroll.js
        Assert.Contains("25", scrollJs);
        Assert.Contains("50", scrollJs);
        Assert.Contains("75", scrollJs);
        Assert.Contains("100", scrollJs);
    }

    // ── T5_05 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T5_05_AllThreeEndpoints_RejectForeignOrigin()
    {
        var a = await Post("/api/tracking/scroll",      new { samples = Array.Empty<object>() },  origin: "https://evil.example.com");
        var b = await Post("/api/tracking/mouse-trail", new { points  = Array.Empty<object>() },  origin: "https://evil.example.com");
        var c = await Post("/api/tracking/form-events", new { events  = Array.Empty<object>() },  origin: "https://evil.example.com");
        Assert.Equal(HttpStatusCode.Forbidden, a.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, b.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, c.StatusCode);
    }

    // ── T5_06 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T5_06_FunnelAdmin_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/Forms/Funnel");
        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther);
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ── T5_07 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T5_07_AbandonmentAdmin_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/Abandonment");
        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther);
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ── T5_08 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T5_08_ScrollHeatmapAdmin_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/Reports/Heatmaps/Scroll");
        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther);
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
