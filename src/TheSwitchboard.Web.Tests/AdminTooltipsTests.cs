using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// A15 — admin-only inline help tooltips.
///
/// TCPA-oriented admin surfaces use a lot of short labels (CAPTURE RATE,
/// PHONE-CONSENT CAPTURE, SUSPICIOUS BOT RATE) that are legally loaded but
/// mean nothing to an admin who wasn't in the build. Hover-tooltips attached
/// to these headings let a new admin self-orient instead of asking.
///
/// Contract:
///   - A `.tb-help` element lives on each of the 4 Compliance KPI labels.
///   - Each `.tb-help` carries a data-tip="..." attribute whose content is
///     the plain-English explanation.
///   - The shared tooltip stylesheet is loaded from the admin layout so any
///     future admin page can drop in the same element.
/// </summary>
public class AdminTooltipsTests : IClassFixture<AdminTooltipsTests.Factory>
{
    private readonly Factory _factory;
    public AdminTooltipsTests(Factory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
    }

    [Fact]
    public async Task A15_01_CompliancePage_HasHelpTooltip_OnPhoneConsent()
    {
        var authed = await _factory.LoggedInClientAsync();
        var body = await (await authed.GetAsync("/Admin/Reports/Compliance")).Content.ReadAsStringAsync();

        // The phone-consent KPI must carry a tb-help badge with a TCPA-flavored explanation.
        Assert.Contains("class=\"tb-help\"", body);
        Assert.Contains("TCPA",   body);
        Assert.Contains("dialing", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A15_02_CompliancePage_HasTooltipsOnAllFourKpis()
    {
        var authed = await _factory.LoggedInClientAsync();
        var body = await (await authed.GetAsync("/Admin/Reports/Compliance")).Content.ReadAsStringAsync();

        // Four KPIs → four tb-help badges minimum.
        var count = System.Text.RegularExpressions.Regex.Matches(body, @"class=""tb-help""").Count;
        Assert.True(count >= 4, $"Expected ≥ 4 tb-help badges on Compliance, found {count}.");

        // Each KPI's tooltip names the metric it explains in plain English.
        Assert.Contains("Overall capture rate",  body);
        Assert.Contains("Phone-consent capture", body);
        Assert.Contains("Total certs",           body);
        Assert.Contains("Suspicious bot",        body);
    }

    [Fact]
    public async Task A15_03_AdminLayout_LoadsTooltipStyles()
    {
        var authed = await _factory.LoggedInClientAsync();
        var body = await (await authed.GetAsync("/Admin/Reports/Compliance")).Content.ReadAsStringAsync();

        // Stylesheet hook — either an inline .tb-help rule or a stylesheet
        // reference that bundles it. Either is fine; this guards that no
        // future edit drops the CSS that makes the tooltip visible.
        var hasInlineRule    = body.Contains(".tb-help");
        var hasStylesheet    = body.Contains("admin.css") || body.Contains("/css/admin");
        Assert.True(hasInlineRule || hasStylesheet, "Tooltip stylesheet not loaded on admin layout.");
    }

    public sealed class Factory : SwitchboardWebApplicationFactory
    {
        private readonly string _dbName = "A15-" + Guid.NewGuid();
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
