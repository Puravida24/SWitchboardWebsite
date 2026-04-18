using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Content;

namespace TheSwitchboard.Web.Pages.Admin.Partners;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) { _db = db; }

    public List<ClientLogo> Partners { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Partners = await _db.ClientLogos.OrderBy(p => p.SortOrder).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var maxOrder = await _db.ClientLogos.MaxAsync(p => (int?)p.SortOrder) ?? 0;
            _db.ClientLogos.Add(new ClientLogo
            {
                CompanyName = name.Trim(),
                LogoUrl = "/uploads/partners/placeholder.png",
                SortOrder = maxOrder + 1,
                IsActive = true
            });
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var p = await _db.ClientLogos.FindAsync(id);
        if (p is not null)
        {
            p.IsActive = !p.IsActive;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var p = await _db.ClientLogos.FindAsync(id);
        if (p is not null)
        {
            _db.ClientLogos.Remove(p);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
