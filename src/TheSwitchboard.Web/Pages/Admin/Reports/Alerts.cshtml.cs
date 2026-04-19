using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class AlertsModel : PageModel
{
    private readonly AppDbContext _db;
    public AlertsModel(AppDbContext db) { _db = db; }

    public IReadOnlyList<AlertRule> Rules { get; private set; } = Array.Empty<AlertRule>();
    public IReadOnlyList<AlertLog> RecentLogs { get; private set; } = Array.Empty<AlertLog>();

    [BindProperty] public string NewName { get; set; } = "";
    [BindProperty] public string NewMetric { get; set; } = "js-errors-1h";
    [BindProperty] public string NewComparison { get; set; } = "gt";
    [BindProperty] public double NewThreshold { get; set; } = 50;
    [BindProperty] public string NewWindow { get; set; } = "1h";

    public async Task OnGetAsync()
    {
        Rules = await _db.AlertRules.OrderByDescending(r => r.CreatedAt).ToListAsync();
        RecentLogs = await _db.AlertLogs.OrderByDescending(l => l.FiredAt).Take(30).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) return RedirectToPage();
        _db.AlertRules.Add(new AlertRule
        {
            Name = NewName.Trim(),
            MetricExpression = NewMetric,
            Comparison = NewComparison,
            Threshold = NewThreshold,
            Window = NewWindow,
            Enabled = true,
            CreatedAt = DateTime.UtcNow
        });
        try { await _db.SaveChangesAsync(); } catch { /* dupe name */ }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(long id)
    {
        var r = await _db.AlertRules.FindAsync(id);
        if (r is not null) { r.Enabled = !r.Enabled; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
