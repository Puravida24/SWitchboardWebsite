using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class DeploysModel : PageModel
{
    private readonly AppDbContext _db;
    public DeploysModel(AppDbContext db) { _db = db; }

    public IReadOnlyList<DeployChange> Rows { get; private set; } = Array.Empty<DeployChange>();

    public async Task OnGetAsync()
    {
        Rows = await _db.DeployChanges.OrderByDescending(d => d.DeployedAt).Take(50).ToListAsync();
    }
}
