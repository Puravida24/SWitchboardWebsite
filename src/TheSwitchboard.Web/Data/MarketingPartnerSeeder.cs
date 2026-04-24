using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Models.Content;

namespace TheSwitchboard.Web.Data;

public static class MarketingPartnerSeeder
{
    public static async Task SeedIfEmptyAsync(IServiceProvider services, IWebHostEnvironment env, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.MarketingPartners.AnyAsync()) return;

        var seedDir = Path.Combine(env.ContentRootPath, "Data", "Seeds");
        var namesPath = Path.Combine(seedDir, "marketing-partners.txt");
        var linksPath = Path.Combine(seedDir, "marketing-partners-links.tsv");

        if (!File.Exists(namesPath))
        {
            logger.LogWarning("MarketingPartnerSeeder: seed file not found at {Path}", namesPath);
            return;
        }

        // Load URL mapping — case-insensitive lookup by partner name.
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

        var lines = await File.ReadAllLinesAsync(namesPath);
        var now = DateTime.UtcNow;
        var rows = lines
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new MarketingPartner
            {
                Name = name,
                WebsiteUrl = urlMap.TryGetValue(name, out var u) ? u : null,
                IsActive = true,
                CreatedAt = now
            })
            .ToList();

        db.MarketingPartners.AddRange(rows);
        await db.SaveChangesAsync();
        var linked = rows.Count(r => !string.IsNullOrEmpty(r.WebsiteUrl));
        logger.LogInformation("MarketingPartnerSeeder: inserted {Count} partners ({Linked} hyperlinked)", rows.Count, linked);
    }
}
