using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// H-4b — a Backfill-30d button on /Admin/Reports/Exports that replays the
/// last 30 days of daily rollups. Useful when the nightly 02:00 UTC
/// RollupService has missed one or more days (container restart, deploy
/// race, pg outage) and the admin dashboards show a gap.
///
/// Uses the existing IRollupRunner.RollupRangeAsync — no new runtime code.
/// </summary>
public class BackfillButtonTests : IClassFixture<BackfillButtonTests.Factory>
{
    private readonly Factory _factory;
    public BackfillButtonTests(Factory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
    }

    [Fact]
    public async Task H4b_01_ExportsPage_RendersBackfillButton()
    {
        var authed = await _factory.LoggedInClientAsync();
        var body = await (await authed.GetAsync("/Admin/Reports/Exports")).Content.ReadAsStringAsync();

        Assert.Contains("Backfill", body);
        // Razor expands asp-page-handler="Backfill30d" into action="/...?handler=Backfill30d".
        Assert.Contains("handler=Backfill30d", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task H4b_02_PostBackfill30d_CallsRollupRange_AndRedirects()
    {
        var authed = await _factory.LoggedInClientAsync();

        // Get antiforgery token from the page
        var pageHtml = await (await authed.GetAsync("/Admin/Reports/Exports")).Content.ReadAsStringAsync();
        var token = Regex.Match(pageHtml, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token), "No antiforgery token on page.");

        using var req = new HttpRequestMessage(HttpMethod.Post, "/Admin/Reports/Exports?handler=Backfill30d")
        {
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            })
        };
        var res = await authed.SendAsync(req);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        // The runner should have been invoked — LastRunAt is fresh (within 1 min).
        using var scope = _factory.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IRollupRunner>();
        Assert.NotNull(runner.LastRunAt);
        Assert.True((DateTime.UtcNow - runner.LastRunAt!.Value).TotalMinutes < 1);
    }

    public sealed class Factory : SwitchboardWebApplicationFactory
    {
        private readonly string _dbName = "H4b-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:InMemoryName"] = _dbName
                });
            });
        }

        public async Task<HttpClient> LoggedInClientAsync()
        {
            var slice4 = new Slice4Factory();
            return await slice4.LoggedInClientAsync();
        }
    }
}
