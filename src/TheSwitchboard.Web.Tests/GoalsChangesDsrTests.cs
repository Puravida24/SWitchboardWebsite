using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice T-11 — Goals + Changes Log + DSR + Deploys.
///
///   T11_01 — A "contact form submit" goal evaluates on FormSubmission insert
///            and produces a GoalConversion row.
///   T11_02 — POST /api/ops/deploy-change without bearer → 401, with bearer → 201.
///   T11_03 — DsrService.DeleteByEmailAsync erases submissions + sessions +
///            clicks for a matching email hash, logs per-table counts.
///   T11_04..06 — /Admin/Reports/Goals, /ChangesLog, /DSR anon redirect.
/// </summary>
public class GoalsChangesDsrTests : IClassFixture<GoalsChangesDsrTests.KeyedFactory>
{
    private readonly KeyedFactory _factory;
    private readonly HttpClient _client;
    private const string DeployKey = "deploy-token-xyz-test";

    public sealed class KeyedFactory : SwitchboardWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ops:DeployChangeToken"] = DeployKey
                }));
        }
    }

    public GoalsChangesDsrTests(KeyedFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ── T11_01 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T11_01_FormSubmitGoal_FiresConversion()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<TheSwitchboard.Web.Services.Tracking.IGoalService>();

        var goal = new Goal
        {
            Name = "contact-submit",
            Kind = "form",
            MatchExpression = "contact",
            Enabled = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Goals.Add(goal);
        await db.SaveChangesAsync();

        var sub = new TheSwitchboard.Web.Models.Forms.FormSubmission
        {
            FormType = "contact",
            Data = "{}",
            CreatedAt = DateTime.UtcNow
        };
        db.FormSubmissions.Add(sub);
        await db.SaveChangesAsync();

        await svc.EvaluateFormSubmissionAsync(sub, "test-sid", "test-vid");

        var conv = await db.GoalConversions.FirstOrDefaultAsync(c => c.GoalId == goal.Id);
        Assert.NotNull(conv);
        Assert.Equal("test-vid", conv!.VisitorId);
    }

    // ── T11_02 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T11_02_DeployChangeApi_AuthRequired()
    {
        var body = JsonSerializer.Serialize(new { sha = "abc123", summary = "test", category = "tracking" });

        using var noAuth = new HttpRequestMessage(HttpMethod.Post, "/api/ops/deploy-change");
        noAuth.Content = new StringContent(body, Encoding.UTF8, "application/json");
        var unauth = await _client.SendAsync(noAuth);
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        using var authed = new HttpRequestMessage(HttpMethod.Post, "/api/ops/deploy-change");
        authed.Headers.Authorization = new AuthenticationHeaderValue("Bearer", DeployKey);
        authed.Content = new StringContent(body, Encoding.UTF8, "application/json");
        var ok = await _client.SendAsync(authed);
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.DeployChanges.AnyAsync(d => d.Sha == "abc123"));
    }

    // ── T11_03 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task T11_03_DsrDelete_ErasesAcrossTables()
    {
        var email = "dsr-target-" + Guid.NewGuid().ToString("N")[..6] + "@x.com";
        using (var seed = _factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<AppDbContext>();
            db.FormSubmissions.Add(new TheSwitchboard.Web.Models.Forms.FormSubmission
            {
                FormType = "contact",
                Data = "{\"email\":\"" + email + "\"}",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<TheSwitchboard.Web.Services.Tracking.IDsrService>();
        var result = await svc.DeleteByEmailAsync(email);
        Assert.True(result.DeletedRowCounts["FormSubmissions"] >= 1);

        using var verify = _factory.Services.CreateScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db2.FormSubmissions.AnyAsync(s => s.Data.Contains(email)));
        Assert.True(await db2.DataSubjectRequests.AnyAsync());
    }

    // ── T11_04..06 ─────────────────────────────────────────────────────
    [Theory]
    [InlineData("/Admin/Reports/Goals")]
    [InlineData("/Admin/Reports/ChangesLog")]
    [InlineData("/Admin/Reports/Deploys")]
    [InlineData("/Admin/Reports/DSR")]
    public async Task T11_AdminPages_Anonymous_RedirectsToLogin(string path)
    {
        var res = await _client.GetAsync(path);
        Assert.True(res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.SeeOther);
        Assert.Contains("/Admin/Login", res.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
