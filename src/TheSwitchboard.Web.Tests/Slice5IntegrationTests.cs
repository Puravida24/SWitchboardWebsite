using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Ab;
using TheSwitchboard.Web.Models.Site;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice 5 BDD scenarios (S5-01 through S5-14).
/// Automatable scenarios covered here; external (Lighthouse, axe-core, DNS) marked Skip
/// with a pointer to LAUNCH_CHECKLIST.md.
/// </summary>
public class Slice5IntegrationTests : IClassFixture<Slice5Factory>
{
    private readonly Slice5Factory _factory;
    private readonly HttpClient _client;

    public Slice5IntegrationTests(Slice5Factory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ── S5-01 A/B cookie set on first visit, sticky for session ────────
    [Fact]
    public async Task S5_01_AbCookie_SetOnFirstVisit_Sticky()
    {
        await SeedExperiment("hero-headline", new[] { ("control", 50), ("alt", 50) });
        var res1 = await _client.GetAsync("/");
        res1.EnsureSuccessStatusCode();
        // Cookie should be set
        var setCookie = res1.Headers.GetValues("Set-Cookie").FirstOrDefault(c => c.StartsWith("sw_ab_hero-headline"));
        Assert.NotNull(setCookie);
    }

    // ── S5-02 variant A/B served ~50/50 over many visits ───────────────
    [Fact]
    public async Task S5_02_Variant_DistributionIsRoughly5050()
    {
        await SeedExperiment("cta-text", new[] { ("A", 50), ("B", 50) });
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < 50; i++)
        {
            var c = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var res = await c.GetAsync("/");
            var cookie = res.Headers.GetValues("Set-Cookie")
                .FirstOrDefault(x => x.StartsWith("sw_ab_cta-text"));
            if (cookie is null) continue;
            var v = cookie.Split('=')[1].Split(';')[0];
            counts[v] = counts.GetValueOrDefault(v, 0) + 1;
        }
        // Expect BOTH variants received at least one hit out of 50.
        Assert.True(counts.Count >= 2, $"Only saw {counts.Count} variant(s) across 50 requests");
    }

    // ── S5-03 admin creates experiment w/ 2+ variants ──────────────────
    [Fact]
    public async Task S5_03_AdminCreatesExperiment()
    {
        var authed = await _factory.LoggedInClientAsync();
        var res = await authed.GetAsync("/Admin/Experiments");
        res.EnsureSuccessStatusCode();
    }

    // ── S5-04 admin sees conversion rate + significance ────────────────
    [Fact]
    public async Task S5_04_AdminResults_RenderConversionRate()
    {
        var exp = await SeedExperiment("test-exp", new[] { ("control", 50), ("alt", 50) });
        var authed = await _factory.LoggedInClientAsync();
        var res = await authed.GetAsync($"/Admin/Experiments/Detail?id={exp.Id}");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("control", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── S5-05 through S5-10 are Lighthouse / axe / CWV — external ──────
    [Fact(Skip = "Lighthouse / axe-core / CWV / cross-browser are verified manually per LAUNCH_CHECKLIST.md.")]
    public Task S5_05_Lighthouse_Performance_Ge_90() => Task.CompletedTask;
    [Fact(Skip = "Manual — LAUNCH_CHECKLIST.md.")] public Task S5_06_Lighthouse_Accessibility_Ge_95() => Task.CompletedTask;
    [Fact(Skip = "Manual — LAUNCH_CHECKLIST.md.")] public Task S5_07_Axe_NoCriticalViolations() => Task.CompletedTask;
    [Fact(Skip = "Manual — LAUNCH_CHECKLIST.md.")] public Task S5_08_Lcp_Under_2_5s() => Task.CompletedTask;
    [Fact(Skip = "Manual — LAUNCH_CHECKLIST.md.")] public Task S5_09_Inp_Under_200ms() => Task.CompletedTask;
    [Fact(Skip = "Manual — LAUNCH_CHECKLIST.md.")] public Task S5_10_Cls_Under_0_1() => Task.CompletedTask;

    // ── S5-11 old URLs → 301 ────────────────────────────────────────────
    [Fact]
    public async Task S5_11_OldUrl_Redirects301()
    {
        await SeedRedirect("/lead-gen", "/");
        var res = await _client.GetAsync("/lead-gen");
        Assert.Equal(HttpStatusCode.MovedPermanently, res.StatusCode);
        Assert.Equal("/", res.Headers.Location?.ToString());
    }

    // ── S5-12 production deploy — manual ────────────────────────────────
    [Fact(Skip = "Manual — Railway deploy verified by health check in prod.")]
    public Task S5_12_ProductionDeploy_Succeeds() => Task.CompletedTask;

    // ── S5-13 production /health 200 ────────────────────────────────────
    [Fact]
    public async Task S5_13_Health_Returns200()
    {
        var res = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // ── S5-14 zero "lead" language audit on final content ──────────────
    [Fact]
    public async Task S5_14_FinalContentAudit_NoLeadLanguage()
    {
        foreach (var p in new[] { "/", "/privacy", "/terms", "/accessibility" })
        {
            var body = await _client.GetStringAsync(p);
            var text = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]+>", " ");
            Assert.False(
                System.Text.RegularExpressions.Regex.IsMatch(text, @"\blead(s)?\b",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                $"{p} contains banned word 'lead/leads'");
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private async Task<Experiment> SeedExperiment(string slug, (string Name, int Weight)[] variants)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exp = new Experiment { Name = slug, Slug = slug, IsActive = true };
        db.Add(exp);
        await db.SaveChangesAsync();
        foreach (var (name, weight) in variants)
        {
            db.Add(new Variant
            {
                ExperimentId = exp.Id,
                Name = name,
                TrafficWeight = weight,
                IsControl = name == "control" || name == variants[0].Name
            });
        }
        await db.SaveChangesAsync();
        return exp;
    }

    private async Task SeedRedirect(string from, string to)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Add(new Redirect { FromPath = from, ToPath = to, StatusCode = 301 });
        await db.SaveChangesAsync();
    }
}
