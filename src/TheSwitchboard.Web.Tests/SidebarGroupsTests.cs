using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// H-7b — admin sidebar gets too noisy (30+ links) once all the tracking
/// reports land. Switching each category header (Content / Forms / Reports /
/// Consent · TCPA / Settings) to a collapsible <details> group lets the menu
/// \"roll up\" to just the headings, state persisted per-user in localStorage.
///
/// Default: every group closed. The group containing the current path opens
/// automatically so the admin can always see where they are.
/// </summary>
public class SidebarGroupsTests : IClassFixture<SidebarGroupsTests.Factory>
{
    private readonly Factory _factory;
    public SidebarGroupsTests(Factory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
    }

    [Fact]
    public async Task H7b_01_Sidebar_WrapsCategoriesInCollapsibleDetails()
    {
        var authed = await _factory.LoggedInClientAsync();
        var body = await (await authed.GetAsync("/Admin/Dashboard")).Content.ReadAsStringAsync();

        // Every group header is now a <summary> inside <details class="sb-group">.
        Assert.Contains("class=\"sb-group\"", body);
        Assert.Contains("<summary", body, StringComparison.OrdinalIgnoreCase);

        // All 5 category headers are wrapped, not free-floating <div>s.
        foreach (var category in new[] { "Content", "Forms", "Reports", "Consent", "Settings" })
        {
            Assert.Contains(category, body);
        }
    }

    [Fact]
    public async Task H7b_02_ActiveGroup_IsOpenByDefault_WhenOnOverviewPage()
    {
        // On /Admin/Reports/Overview, the \"Reports\" group's <details> must be open
        // so the user can see where they are. Other groups stay collapsed.
        var authed = await _factory.LoggedInClientAsync();
        var body = await (await authed.GetAsync("/Admin/Reports/Overview")).Content.ReadAsStringAsync();

        // Reports group <details> has the open attribute (auto-opened on match).
        Assert.Matches(@"<details[^>]*data-group-id=""reports""[^>]*\sopen", body);

        // Content group does NOT have open — it's collapsed by default.
        var contentMatch = System.Text.RegularExpressions.Regex.Match(
            body, @"<details[^>]*data-group-id=""content""[^>]*>");
        Assert.True(contentMatch.Success, "Content group <details> missing.");
        Assert.DoesNotContain(" open", contentMatch.Value);
    }

    [Fact]
    public async Task H7b_03_SidebarPersistenceScript_IsLoaded()
    {
        var authed = await _factory.LoggedInClientAsync();
        var body = await (await authed.GetAsync("/Admin/Dashboard")).Content.ReadAsStringAsync();

        // The small JS that reads/writes localStorage for each group's state
        // is loaded via /js/admin/sidebar.js (self-hosted, nonce'd).
        Assert.Contains("/js/admin/sidebar.js", body);
    }

    public sealed class Factory : SwitchboardWebApplicationFactory
    {
        private readonly string _dbName = "H7b-" + Guid.NewGuid();
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
