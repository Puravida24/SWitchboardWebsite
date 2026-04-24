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
}
