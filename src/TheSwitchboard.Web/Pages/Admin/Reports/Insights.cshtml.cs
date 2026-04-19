using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class InsightsModel : PageModel
{
    private readonly IInsightsService _svc;
    public InsightsModel(IInsightsService svc) { _svc = svc; }

    public IReadOnlyList<Insight> Rows { get; private set; } = Array.Empty<Insight>();

    public async Task OnGetAsync()
    {
        Rows = await _svc.RecentAsync();
    }

    public async Task<IActionResult> OnPostRunNowAsync()
    {
        await _svc.DetectAsync();
        return RedirectToPage();
    }
}
