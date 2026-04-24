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

        var existingRows = await db.MarketingPartners.ToListAsync();
        var existingByName = new Dictionary<string, MarketingPartner>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in existingRows) existingByName.TryAdd(r.Name, r);

        // Backfill WebsiteUrl on existing rows whenever the TSV has a mapping and the
        // current value is empty or out of date. Prevents "row seeded before the TSV
        // entry existed" from staying unlinked forever.
        var now = DateTime.UtcNow;
        var updatedUrls = 0;
        foreach (var (mappedName, mappedUrl) in urlMap)
        {
            if (!existingByName.TryGetValue(mappedName, out var row)) continue;
            if (row.WebsiteUrl == mappedUrl) continue;
            row.WebsiteUrl = mappedUrl;
            row.UpdatedAt = now;
            updatedUrls++;
        }

        var toInsert = fileNames
            .Where(n => !existingByName.ContainsKey(n))
            .Select(name => new MarketingPartner
            {
                Name = name,
                WebsiteUrl = urlMap.TryGetValue(name, out var u) ? u : null,
                IsActive = true,
                CreatedAt = now
            })
            .ToList();

        if (toInsert.Count == 0 && updatedUrls == 0)
        {
            logger.LogInformation("MarketingPartnerSeeder: nothing to change ({Total} partners, seed file + link map in sync)", existingRows.Count);
            return;
        }

        if (toInsert.Count > 0) db.MarketingPartners.AddRange(toInsert);
        await db.SaveChangesAsync();

        var linked = toInsert.Count(r => !string.IsNullOrEmpty(r.WebsiteUrl));
        logger.LogInformation(
            "MarketingPartnerSeeder: inserted {Inserted} missing ({Linked} hyperlinked), backfilled URLs on {Updated} existing; {Total} total after seed",
            toInsert.Count, linked, updatedUrls, existingRows.Count + toInsert.Count);
    }
}
