using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class GoalsModel : PageModel
{
    private readonly AppDbContext _db;
    public GoalsModel(AppDbContext db) { _db = db; }

    public IReadOnlyList<Goal> Goals { get; private set; } = Array.Empty<Goal>();
    public IDictionary<long, int> ConversionCounts { get; private set; } = new Dictionary<long, int>();

    [BindProperty] public string NewName { get; set; } = "";
    [BindProperty] public string NewKind { get; set; } = "form";
    [BindProperty] public string? NewMatch { get; set; }

    public async Task OnGetAsync()
    {
        Goals = await _db.Goals.OrderByDescending(g => g.CreatedAt).ToListAsync();
        var counts = await _db.GoalConversions
            .GroupBy(c => c.GoalId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        ConversionCounts = counts.ToDictionary(c => c.Key, c => c.Count);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) return RedirectToPage();
        _db.Goals.Add(new Goal
        {
            Name = NewName.Trim(),
            Kind = NewKind,
            MatchExpression = NewMatch,
            Enabled = true,
            CreatedAt = DateTime.UtcNow
        });
        try { await _db.SaveChangesAsync(); } catch { /* duplicate name, ignore */ }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(long id)
    {
        var goal = await _db.Goals.FindAsync(id);
        if (goal is not null) { goal.Enabled = !goal.Enabled; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
