using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice T-9 — Overview + Trends + Visitors + Cohorts + Engagement.
///
///   T9_01 — OverviewService returns a sane empty report when no data exists.
///   T9_02 — VisitorProfileService returns all sessions for a vid.
///   T9_03 — CohortService computes retention across a seeded dataset.
///   T9_04..08 — 5 admin pages anon redirect to /Admin/Login.
///   T9_09 — /js/vendor/chart.min.js is vendored.
/// </summary>
public class ReportsUiTests : IClassFixture<SwitchboardWebApplicationFactory>
{
    private readonly SwitchboardWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsUiTests(SwitchboardWebApplicationFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ── T9_01 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T9_01_OverviewService_ReturnsSaneEmptyReport()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IOverviewService>();
        var report = await svc.GetAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
        Assert.NotNull(report);
        Assert.True(report.HealthScore >= 0 && report.HealthScore <= 100);
        Assert.NotNull(report.TopPages);
        Assert.NotNull(report.TopCampaigns);
        Assert.NotNull(report.TopErrors);
    }

    // ── T9_02 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T9_02_VisitorProfile_ReturnsAllSessions()
    {
        var vid = "t9_vid_" + Guid.NewGuid().ToString("N")[..10];
        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Visitors.Add(new Visitor { Id = vid, FirstSeen = DateTime.UtcNow.AddDays(-5), LastSeen = DateTime.UtcNow, SessionCount = 3 });
            for (var i = 0; i < 3; i++)
            {
                db.Sessions.Add(new Session
                {
                    Id = $"{vid}_s{i}",
                    VisitorId = vid,
                    StartedAt = DateTime.UtcNow.AddDays(-i),
                    EndedAt = DateTime.UtcNow.AddDays(-i).AddMinutes(2),
                    PageCount = i + 1,
                    LandingPath = "/"
                });
            }
            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IVisitorAnalyticsService>();
        var profile = await svc.GetProfileAsync(vid);
        Assert.NotNull(profile);
        Assert.Equal(vid, profile!.Visitor.Id);
        Assert.Equal(3, profile.Sessions.Count);
    }

    // ── T9_03 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T9_03_Cohorts_ComputeRetention()
    {
        // Seed: cohort week A has 2 unique visitors; both return in week A+1 (100% W1),
        // one returns in A+2 (50% W2).
        var baseWeek = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc); // Sunday
        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            var vA = "t9c_" + Guid.NewGuid().ToString("N")[..8];
            var vB = "t9c_" + Guid.NewGuid().ToString("N")[..8];
            db.Visitors.AddRange(
                new Visitor { Id = vA, FirstSeen = baseWeek.AddDays(1), LastSeen = baseWeek.AddDays(15), SessionCount = 3 },
                new Visitor { Id = vB, FirstSeen = baseWeek.AddDays(2), LastSeen = baseWeek.AddDays(8),  SessionCount = 2 });
            // Sessions: vA weeks 0, 1, 2; vB weeks 0, 1.
            db.Sessions.AddRange(
                new Session { Id = "c_" + Guid.NewGuid().ToString("N")[..8], VisitorId = vA, StartedAt = baseWeek.AddDays(1),  EndedAt = baseWeek.AddDays(1)  },
                new Session { Id = "c_" + Guid.NewGuid().ToString("N")[..8], VisitorId = vA, StartedAt = baseWeek.AddDays(8),  EndedAt = baseWeek.AddDays(8)  },
                new Session { Id = "c_" + Guid.NewGuid().ToString("N")[..8], VisitorId = vA, StartedAt = baseWeek.AddDays(15), EndedAt = baseWeek.AddDays(15) },
                new Session { Id = "c_" + Guid.NewGuid().ToString("N")[..8], VisitorId = vB, StartedAt = baseWeek.AddDays(2),  EndedAt = baseWeek.AddDays(2)  },
                new Session { Id = "c_" + Guid.NewGuid().ToString("N")[..8], VisitorId = vB, StartedAt = baseWeek.AddDays(9),  EndedAt = baseWeek.AddDays(9)  });
            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICohortService>();
        var rows = await svc.WeeklyCohortsAsync(baseWeek, 3);
        var cohort = rows.FirstOrDefault(r => r.CohortStart.Date == baseWeek.Date);
        Assert.NotNull(cohort);
        Assert.True(cohort!.Size >= 2);
        // Week 0 retention = 100% by definition.
        Assert.Equal(100, cohort.WeekRetentionPct[0]);
    }

    // ── T9_04..08 ──────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Admin/Reports/Overview")]
    [InlineData("/Admin/Reports/Trends")]
    [InlineData("/Admin/Reports/Visitors")]
    [InlineData("/Admin/Reports/Visitors/Profile?id=x")]
    [InlineData("/Admin/Reports/Cohorts")]
    [InlineData("/Admin/Reports/Engagement")]
    public async Task T9_AdminPages_Anonymous_RedirectsToLogin(string path)
    {
        var res = await _client.GetAsync(path);
        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther,
            $"{path}: expected redirect, got {(int)res.StatusCode}");
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ── T9_09 ──────────────────────────────────────────────────────────
    [Fact]
    public async Task T9_09_ChartJs_Vendored()
    {
        var res = await _client.GetAsync("/js/vendor/chart.min.js");
        Assert.True(res.IsSuccessStatusCode, $"chart.min.js must be vendored (got {(int)res.StatusCode})");
    }
}
