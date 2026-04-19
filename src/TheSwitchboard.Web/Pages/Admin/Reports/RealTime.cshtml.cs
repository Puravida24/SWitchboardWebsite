using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class RealTimeModel : PageModel
{
    private readonly IRealtimeMetrics _metrics;
    public RealTimeModel(IRealtimeMetrics metrics) { _metrics = metrics; }

    public int InitialVisitorCount { get; private set; }

    public void OnGet()
    {
        InitialVisitorCount = _metrics.ActiveVisitorCount();
    }

    public IActionResult OnGetStats()
    {
        return new JsonResult(new { active = _metrics.ActiveVisitorCount() });
    }
}
