using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Models.Content;

namespace TheSwitchboard.Web.Data;

public static class MarketingPartnerSeeder
{
    /// <summary>
    /// Inserts any names from <c>Data/Seeds/marketing-partners.txt</c> that aren't already in
    /// the <see cref="AppDbContext.MarketingPartners"/> table. Idempotent: re-running does nothing
    /// once the seed file and table agree. Existing rows are left untouched (name + URL only
    /// flow in; admin-managed state like IsActive is not overwritten once set).
    /// </summary>
    public static async Task SeedMissingAsync(IServiceProvider services, IWebHostEnvironment env, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seedDir = Path.Combine(env.ContentRootPath, "Data", "Seeds");
        var namesPath = Path.Combine(seedDir, "marketing-partners.txt");
        var linksPath = Path.Combine(seedDir, "marketing-partners-links.tsv");

        if (!File.Exists(namesPath))
        {
            logger.LogWarning("MarketingPartnerSeeder: seed file not found at {Path}", namesPath);
            return;
        }

        var urlMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(linksPath))
        {
            foreach (var raw in await File.ReadAllLinesAsync(linksPath))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                var parts = line.Split('\t', 2);
                if (parts.Length != 2) continue;
                var name = parts[0].Trim();
                var url  = parts[1].Trim();
                if (name.Length > 0 && url.Length > 0) urlMap[name] = url;
            }
        }

        var fileNames = (await File.ReadAllLinesAsync(namesPath))
            .Select(l => l.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = new HashSet<string>(
            await db.MarketingPartners.Select(p => p.Name).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var toInsert = fileNames
            .Where(n => !existing.Contains(n))
            .Select(name => new MarketingPartner
            {
                Name = name,
                WebsiteUrl = urlMap.TryGetValue(name, out var u) ? u : null,
                IsActive = true,
                CreatedAt = now
            })
            .ToList();

        if (toInsert.Count == 0)
        {
            logger.LogInformation("MarketingPartnerSeeder: nothing to insert ({Total} partners, seed file in sync)", existing.Count);
            return;
        }

        db.MarketingPartners.AddRange(toInsert);
        await db.SaveChangesAsync();

        var linked = toInsert.Count(r => !string.IsNullOrEmpty(r.WebsiteUrl));
        logger.LogInformation(
            "MarketingPartnerSeeder: inserted {Inserted} missing partners ({Linked} hyperlinked); {Total} total after seed",
            toInsert.Count, linked, existing.Count + toInsert.Count);
    }
}
