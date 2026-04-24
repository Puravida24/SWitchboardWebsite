using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Content;

namespace TheSwitchboard.Web.Pages.Admin.MarketingPartners;

public class IndexModel : PageModel
{
    private const int PageSize = 100;

    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public IReadOnlyList<MarketingPartner> Partners { get; private set; } = Array.Empty<MarketingPartner>();
    public int TotalCount { get; private set; }
    public int ActiveCount { get; private set; }
    public int LinkedCount { get; private set; }
    public int PageIndex { get; private set; } = 1;
    public int TotalPages { get; private set; } = 1;
    public string? Query { get; private set; }

    public async Task OnGetAsync(string? q = null, int page = 1)
    {
        Query = q?.Trim();
        PageIndex = Math.Max(1, page);

        var baseQuery = _db.MarketingPartners.AsQueryable();
        if (!string.IsNullOrWhiteSpace(Query))
            baseQuery = baseQuery.Where(p => EF.Functions.Like(p.Name, $"%{Query}%"));

        TotalCount = await baseQuery.CountAsync();
        TotalPages = Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
        if (PageIndex > TotalPages) PageIndex = TotalPages;

        Partners = await baseQuery
            .OrderBy(p => p.Name)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ActiveCount  = await _db.MarketingPartners.CountAsync(p => p.IsActive);
        LinkedCount  = await _db.MarketingPartners.CountAsync(p => p.WebsiteUrl != null && p.WebsiteUrl != "");
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string? websiteUrl)
    {
        if (string.IsNullOrWhiteSpace(name)) return RedirectToPage();
        _db.MarketingPartners.Add(new MarketingPartner
        {
            Name = name.Trim(),
            WebsiteUrl = string.IsNullOrWhiteSpace(websiteUrl) ? null : websiteUrl.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(int id, string name, string? websiteUrl)
    {
        var row = await _db.MarketingPartners.FindAsync(id);
        if (row is null) return RedirectToPage();
        if (!string.IsNullOrWhiteSpace(name)) row.Name = name.Trim();
        row.WebsiteUrl = string.IsNullOrWhiteSpace(websiteUrl) ? null : websiteUrl.Trim();
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var row = await _db.MarketingPartners.FindAsync(id);
        if (row is not null)
        {
            row.IsActive = !row.IsActive;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["ImportResult"] = "No file uploaded.";
            return RedirectToPage();
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var raw = await reader.ReadToEndAsync();

        // Parse CSV: first line is header, one name per subsequent line.
        // Accept either a single-column CSV or a "Name,Url" two-column CSV.
        var lines = raw.Replace("\r\n", "\n").Split('\n');
        var pairs = new List<(string name, string? url)>();
        bool headerSeen = false;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;
            if (!headerSeen)
            {
                headerSeen = true;
                // Skip header if it looks like one (contains "Name" or "name")
                if (line.Contains("name", StringComparison.OrdinalIgnoreCase)) continue;
            }
            var parts = line.Split(',', 2);
            var name = parts[0].Trim().Trim('"');
            var url  = parts.Length > 1 ? parts[1].Trim().Trim('"') : null;
            if (name.Length == 0) continue;
            pairs.Add((name, string.IsNullOrWhiteSpace(url) ? null : url));
        }

        // Dedup incoming against itself first (case-insensitive).
        var distinct = pairs
            .GroupBy(p => p.name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        // Load existing names once for O(1) lookups.
        var existing = new HashSet<string>(
            await _db.MarketingPartners.Select(p => p.Name).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var toInsert = distinct
            .Where(p => !existing.Contains(p.name))
            .Select(p => new MarketingPartner
            {
                Name = p.name,
                WebsiteUrl = p.url,
                IsActive = true,
                CreatedAt = now
            })
            .ToList();
        var skipped = distinct.Count - toInsert.Count;

        if (toInsert.Count > 0)
        {
            _db.MarketingPartners.AddRange(toInsert);
            await _db.SaveChangesAsync();
        }

        TempData["ImportResult"] =
            $"Imported {toInsert.Count} new, skipped {skipped} existing (out of {distinct.Count} unique rows).";
        return RedirectToPage();
    }
}
