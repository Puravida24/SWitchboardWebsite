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
