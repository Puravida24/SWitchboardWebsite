using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class DSRModel : PageModel
{
    private readonly IDsrService _svc;
    private readonly AppDbContext _db;

    public DSRModel(IDsrService svc, AppDbContext db) { _svc = svc; _db = db; }

    public IReadOnlyList<DataSubjectRequest> History { get; private set; } = Array.Empty<DataSubjectRequest>();
    public DsrResult? LastResult { get; private set; }

    [BindProperty] public string Email { get; set; } = "";

    public async Task OnGetAsync()
    {
        History = await _db.DataSubjectRequests.OrderByDescending(r => r.RequestedAt).Take(50).ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        if (string.IsNullOrWhiteSpace(Email)) return RedirectToPage();
        LastResult = await _svc.DeleteByEmailAsync(Email.Trim());
        History = await _db.DataSubjectRequests.OrderByDescending(r => r.RequestedAt).Take(50).ToListAsync();
        return Page();
    }
}
