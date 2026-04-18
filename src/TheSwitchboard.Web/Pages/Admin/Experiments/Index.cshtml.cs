using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Ab;

namespace TheSwitchboard.Web.Pages.Admin.Experiments;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) { _db = db; }

    public List<Experiment> Experiments { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Experiments = await _db.Experiments.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }
}
