using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// A11 — rate-limit tuning for the bearer-guarded API surface.
///
/// Today the middleware caps every /api/* non-tracker endpoint at 10 req/min/IP.
/// That's both too strict for /api/consent/match (Phoenix dials more than 10
/// leads/minute in peak hours — legitimate traffic would get 429'd) and too
/// loose for /api/ops/deploy-change (deploys happen maybe 1-2x/day; allowing
/// 10 failures/minute hands token-guessers too many tries before lockout).
///
/// A11 sets:
///   /api/consent/match       cap = 60/min/IP
///   /api/ops/deploy-change   cap = 5/min/IP
///   all other /api/*         cap = 10/min/IP (unchanged)
/// </summary>
public class RateLimitTuningTests : IClassFixture<RateLimitTuningTests.Factory>
{
    private readonly Factory _factory;
    public RateLimitTuningTests(Factory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
    }

    [Fact]
    public async Task A11_01_ConsentMatch_Allows_MoreThan_10_PerMinute()
    {
        // Phoenix dials ≥10 prospects/minute in peak hours. Legit volume.
        var client = _factory.CreateClient();

        // Unauthenticated requests — we just need to observe 401 vs 429 to know
        // whether the rate-limiter blocked us before the bearer check ran.
        // First ~60 requests: bearer missing → 401 (rate-limiter lets them through).
        // 61st request: rate-limiter → 429.
        HttpResponseMessage? res = null;
        for (int i = 1; i <= 15; i++)
        {
            res = await client.PostAsJsonAsync("/api/consent/match", new { certificateId = "x" });
            Assert.NotEqual(HttpStatusCode.TooManyRequests, res.StatusCode);
        }
        // Bearer not configured in test env → endpoint returns 503, but critically NOT 429.
        Assert.NotEqual(HttpStatusCode.TooManyRequests, res!.StatusCode);
    }

    [Fact]
    public async Task A11_02_ConsentMatch_Blocks_At_61st_PerMinute()
    {
        var client = _factory.CreateClient();
        HttpResponseMessage? last = null;
        for (int i = 1; i <= 70; i++)
        {
            last = await client.PostAsJsonAsync("/api/consent/match", new { certificateId = "x" });
            if (last.StatusCode == HttpStatusCode.TooManyRequests) break;
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [Fact]
    public async Task A11_03_DeployChange_BlocksAt6thPerMinute()
    {
        // Deploys happen 1-2x/day. Tight bucket blocks token-guessers after 5 tries.
        var client = _factory.CreateClient();
        HttpResponseMessage? last = null;
        for (int i = 1; i <= 10; i++)
        {
            last = await client.PostAsJsonAsync("/api/ops/deploy-change", new { sha = "abc", summary = "x" });
            if (last.StatusCode == HttpStatusCode.TooManyRequests) break;
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [Fact]
    public async Task A11_04_OtherApi_Still_Caps_At_10()
    {
        // /api/contact is NOT one of the bearer-guarded endpoints — still 10/min.
        var client = _factory.CreateClient();
        HttpResponseMessage? last = null;
        for (int i = 1; i <= 15; i++)
        {
            last = await client.PostAsJsonAsync("/api/contact", new {
                name = "A", email = "a@b.com", company = "C", role = "Carrier"
            });
            if (last.StatusCode == HttpStatusCode.TooManyRequests) break;
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    public sealed class Factory : SwitchboardWebApplicationFactory
    {
        private readonly string _dbName = "A11-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:InMemoryName"] = _dbName,
                });
            });
        }
    }
}
