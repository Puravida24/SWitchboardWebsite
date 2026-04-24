using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Content;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// MP-1 BDD scenarios for the /marketing-partners public page.
/// RED phase — tests fail until MarketingPartner entity + page ship.
/// </summary>
public class MarketingPartnersIntegrationTests : IClassFixture<MarketingPartnersFactory>
{
    private readonly MarketingPartnersFactory _factory;
    private readonly HttpClient _client;

    public MarketingPartnersIntegrationTests(MarketingPartnersFactory factory)
    {
        _factory = factory;
        TheSwitchboard.Web.Middleware.RateLimitMiddleware.ResetAll();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ── MP-01 page returns 200 ────────────────────────────────────────
    [Fact]
    public async Task MP_01_MarketingPartnersPage_Returns200()
    {
        var res = await _client.GetAsync("/marketing-partners");
        Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
    }

    // ── MP-02 renders seeded partner names ────────────────────────────
    [Fact]
    public async Task MP_02_MarketingPartnersPage_ListsSeededPartners()
    {
        await SeedPartners("Acme Insurance", "Beta Insurance", "Zed Insurance");
        var body = await _client.GetStringAsync("/marketing-partners");
        Assert.Contains("Acme Insurance", body);
        Assert.Contains("Beta Insurance", body);
        Assert.Contains("Zed Insurance", body);
    }

    // ── MP-03 partners render in alphabetical order ───────────────────
    [Fact]
    public async Task MP_03_MarketingPartnersPage_IsAlphabetized()
    {
        await SeedPartners("Zed Insurance", "Acme Insurance", "Middle Insurance");
        var body = await _client.GetStringAsync("/marketing-partners");
        var iA = body.IndexOf("Acme Insurance", StringComparison.Ordinal);
        var iM = body.IndexOf("Middle Insurance", StringComparison.Ordinal);
        var iZ = body.IndexOf("Zed Insurance", StringComparison.Ordinal);
        Assert.True(iA >= 0 && iM >= 0 && iZ >= 0, "all three partner names should appear in the rendered page");
        Assert.True(iA < iM, "Acme should appear before Middle");
        Assert.True(iM < iZ, "Middle should appear before Zed");
    }

    // ── MP-04 brand rule: no "lead/leads" in page copy ────────────────
    [Fact]
    public async Task MP_04_MarketingPartnersPage_NoLeadCopy()
    {
        var body = await _client.GetStringAsync("/marketing-partners");
        var lc = body.ToLowerInvariant();
        // Whole-word match only — avoid "leadership"/"leading" false positives.
        Assert.False(System.Text.RegularExpressions.Regex.IsMatch(lc, @"\blead\b"),
            "Page must not contain the word 'lead' (brand rule).");
        Assert.False(System.Text.RegularExpressions.Regex.IsMatch(lc, @"\bleads\b"),
            "Page must not contain the word 'leads' (brand rule).");
    }

    // ── MP-06 seeder backfills WebsiteUrl on existing rows from link map ──
    [Fact]
    public async Task MP_06_Seeder_BackfillsUrlOnExistingRows()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Arrange: seed "Quote Wizard" without a URL (mirrors prod state where the
        // row was inserted before the TSV mapping existed).
        db.MarketingPartners.RemoveRange(db.MarketingPartners);
        await db.SaveChangesAsync();
        db.MarketingPartners.Add(new MarketingPartner { Name = "Quote Wizard", IsActive = true, WebsiteUrl = null });
        await db.SaveChangesAsync();
        var before = await db.MarketingPartners.FirstAsync(p => p.Name == "Quote Wizard");
        Assert.Null(before.WebsiteUrl);

        // Act
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        await MarketingPartnerSeeder.SeedMissingAsync(_factory.Services, env, logger);

        // Assert: existing "Quote Wizard" row now has the URL from the TSV.
        // Seeder ran in a separate scope — clear the tracker so we re-read from store,
        // not from this scope's cached instance.
        db.ChangeTracker.Clear();
        var after = await db.MarketingPartners.AsNoTracking().FirstAsync(p => p.Name == "Quote Wizard");
        Assert.False(string.IsNullOrEmpty(after.WebsiteUrl),
            "Seeder should backfill WebsiteUrl on existing rows from the TSV link map");
        Assert.Contains("quotewizard.com", after.WebsiteUrl);
    }

    // ── MP-05 seeder inserts missing entries when table is non-empty ──
    [Fact]
    public async Task MP_05_Seeder_InsertsMissingWhenTableNonEmpty()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Arrange: start from a non-empty table (one pre-existing row).
        db.MarketingPartners.RemoveRange(db.MarketingPartners);
        await db.SaveChangesAsync();
        db.MarketingPartners.Add(new MarketingPartner { Name = "Preexisting Test Row", IsActive = true });
        await db.SaveChangesAsync();
        Assert.Equal(1, await db.MarketingPartners.CountAsync());

        // Act: run the seeder — should fill in all missing names from the seed file
        // even though the table is non-empty.
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("test");
        await MarketingPartnerSeeder.SeedMissingAsync(_factory.Services, env, logger);

        // Assert: table now has many more rows (real seed file has 13k+ entries).
        var count = await db.MarketingPartners.CountAsync();
        Assert.True(count > 1000, $"Expected seeder to insert missing rows from file. Got {count}.");
        // And the pre-existing row is still there.
        Assert.True(await db.MarketingPartners.AnyAsync(p => p.Name == "Preexisting Test Row"));
    }

    // ── MP_16 CSV import: inserts new names, skips existing ──────────
    [Fact]
    public async Task MP_16_CsvImport_InsertsNew_SkipsExisting()
    {
        await SeedPartners("Existing Alpha", "Existing Beta");
        var authed = await _factory.LoggedInClientAsync();

        // CSV with 2 new + 2 existing (case-insensitive match on existing).
        var csv = "Name\nNew Gamma\nNew Delta\nexisting alpha\nEXISTING BETA\n";
        var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(ms);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "import.csv");

        // Grab AF token from the admin page first.
        var page = await authed.GetStringAsync("/Admin/MarketingPartners");
        var tokenMatch = System.Text.RegularExpressions.Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        if (tokenMatch.Success)
        {
            content.Add(new StringContent(tokenMatch.Groups[1].Value), "__RequestVerificationToken");
        }
        var res = await authed.PostAsync("/Admin/MarketingPartners?handler=Import", content);
        Assert.True(res.StatusCode is System.Net.HttpStatusCode.Redirect
                                  or System.Net.HttpStatusCode.Found
                                  or System.Net.HttpStatusCode.SeeOther
                                  or System.Net.HttpStatusCode.OK,
            $"Expected redirect or OK after import, got {res.StatusCode}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.MarketingPartners.AnyAsync(p => p.Name == "New Gamma"));
        Assert.True(await db.MarketingPartners.AnyAsync(p => p.Name == "New Delta"));
        // Existing rows still only one copy each (case-insensitive dedup).
        Assert.Equal(1, await db.MarketingPartners.CountAsync(p => p.Name.ToLower() == "existing alpha"));
        Assert.Equal(1, await db.MarketingPartners.CountAsync(p => p.Name.ToLower() == "existing beta"));
    }

    // ── MP_13 Public page ?letter=S filters to S partners only ───────
    [Fact]
    public async Task MP_13_PublicPage_LetterFilter_OnlyShowsThatLetter()
    {
        await SeedPartners("Alpha Co", "Sierra Co", "Sigma Co", "Zed Co");
        var body = await _client.GetStringAsync("/marketing-partners?letter=S");
        Assert.Contains("Sierra Co", body);
        Assert.Contains("Sigma Co", body);
        Assert.DoesNotContain("Alpha Co", body);
        Assert.DoesNotContain("Zed Co", body);
    }

    // ── MP_14 Public page ?q=X filters substring match ──────────────
    [Fact]
    public async Task MP_14_PublicPage_Search_FiltersSubstring()
    {
        await SeedPartners("Allstate Insurance", "State Farm", "GEICO");
        var body = await _client.GetStringAsync("/marketing-partners?q=stat");
        // Case-insensitive substring
        Assert.Contains("Allstate Insurance", body);
        Assert.Contains("State Farm", body);
        Assert.DoesNotContain("GEICO", body);
    }

    // ── MP_15 Public page renders A-Z nav strip ──────────────────────
    [Fact]
    public async Task MP_15_PublicPage_RendersAzNavStrip()
    {
        await SeedPartners("Alpha Co", "Mike Co", "Zulu Co");
        var body = await _client.GetStringAsync("/marketing-partners");
        // Look for the A-Z nav container
        Assert.Contains("class=\"az-nav\"", body);
        // Expect both enabled letters and at least one disabled class since not all letters have entries
        Assert.Contains("az-nav-link", body);
        Assert.Contains("az-nav-disabled", body);
    }

    // ── MP_07 Admin list page requires auth ──────────────────────────
    [Fact]
    public async Task MP_07_AdminList_Anonymous_RedirectsToLogin()
    {
        var res = await _client.GetAsync("/Admin/MarketingPartners");
        Assert.True(res.StatusCode is System.Net.HttpStatusCode.Redirect
                                  or System.Net.HttpStatusCode.Found
                                  or System.Net.HttpStatusCode.SeeOther,
            $"Expected redirect, got {res.StatusCode}");
        Assert.Contains("/Admin/Login",
            res.Headers.Location?.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    // ── MP_08 Authenticated admin list page renders + shows partners ──
    [Fact]
    public async Task MP_08_AdminList_Authenticated_RendersPartners()
    {
        await SeedPartners("Admin Test Partner One", "Admin Test Partner Two");
        var authed = await _factory.LoggedInClientAsync();
        var body = await authed.GetStringAsync("/Admin/MarketingPartners");
        Assert.Contains("Admin Test Partner One", body);
        Assert.Contains("Admin Test Partner Two", body);
    }

    // ── MP_09 Admin can search the partner list ──────────────────────
    [Fact]
    public async Task MP_09_AdminList_Search_FiltersResults()
    {
        await SeedPartners("Alpha Unique", "Beta Unique", "Gamma Unique");
        var authed = await _factory.LoggedInClientAsync();
        var body = await authed.GetStringAsync("/Admin/MarketingPartners?q=Beta");
        Assert.Contains("Beta Unique", body);
        Assert.DoesNotContain("Alpha Unique", body);
        Assert.DoesNotContain("Gamma Unique", body);
    }

    // ── MP_10 Admin can create a new partner ────────────────────────
    [Fact]
    public async Task MP_10_AdminCreate_PersistsPartner()
    {
        var authed = await _factory.LoggedInClientAsync();
        await _factory.PostAsync(authed, "/Admin/MarketingPartners?handler=Create", new()
        {
            ["Name"] = "Newly Created Partner",
            ["WebsiteUrl"] = "https://example.com/mp"
        });
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.MarketingPartners.FirstOrDefaultAsync(p => p.Name == "Newly Created Partner");
        Assert.NotNull(row);
        Assert.Equal("https://example.com/mp", row!.WebsiteUrl);
        Assert.True(row.IsActive);
    }

    // ── MP_11 Admin can toggle IsActive ──────────────────────────────
    [Fact]
    public async Task MP_11_AdminToggle_FlipsIsActive()
    {
        await SeedPartners("Toggle Target");
        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = (await db.MarketingPartners.FirstAsync(p => p.Name == "Toggle Target")).Id;
        }
        var authed = await _factory.LoggedInClientAsync();
        await _factory.PostAsync(authed, $"/Admin/MarketingPartners?handler=Toggle&id={id}", new());
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.MarketingPartners.FindAsync(id);
            Assert.NotNull(row);
            Assert.False(row!.IsActive, "Toggle should flip IsActive to false");
        }
    }

    // ── MP_12 Admin can edit name + URL ──────────────────────────────
    [Fact]
    public async Task MP_12_AdminEdit_PersistsChanges()
    {
        await SeedPartners("Original Name");
        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = (await db.MarketingPartners.FirstAsync(p => p.Name == "Original Name")).Id;
        }
        var authed = await _factory.LoggedInClientAsync();
        await _factory.PostAsync(authed, $"/Admin/MarketingPartners?handler=Edit&id={id}", new()
        {
            ["Name"] = "Updated Name",
            ["WebsiteUrl"] = "https://updated.example.com"
        });
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.MarketingPartners.FindAsync(id);
            Assert.Equal("Updated Name", row!.Name);
            Assert.Equal("https://updated.example.com", row.WebsiteUrl);
        }
    }

    private async Task SeedPartners(params string[] names)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.MarketingPartners.RemoveRange(db.MarketingPartners);
        await db.SaveChangesAsync();
        foreach (var n in names)
        {
            db.MarketingPartners.Add(new MarketingPartner { Name = n, IsActive = true });
        }
        await db.SaveChangesAsync();
    }
}
