using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Content;

namespace TheSwitchboard.Web.Pages.Admin;

public class HistoryModel : PageModel
{
    private readonly AppDbContext _db;

    public HistoryModel(AppDbContext db) { _db = db; }

    [BindProperty(SupportsGet = true)] public string? Type { get; set; }
    [BindProperty(SupportsGet = true)] public string? Key { get; set; }
    [BindProperty(SupportsGet = true)] public string? Field { get; set; }

    public List<ContentVersion> Versions { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var q = _db.ContentVersions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(Type)) q = q.Where(v => v.EntityType == Type);
        if (!string.IsNullOrWhiteSpace(Key)) q = q.Where(v => v.EntityKey == Key);
        if (!string.IsNullOrWhiteSpace(Field)) q = q.Where(v => v.FieldName == Field);
        Versions = await q.OrderByDescending(v => v.UpdatedAt).Take(10).ToListAsync();
    }
}
