using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class CohortsModel : PageModel
{
    private readonly ICohortService _svc;
    public CohortsModel(ICohortService svc) { _svc = svc; }

    public int WeeksCount { get; private set; } = 8;
    public IReadOnlyList<CohortRow> Rows { get; private set; } = Array.Empty<CohortRow>();

    public async Task OnGetAsync(int? weeks)
    {
        WeeksCount = weeks is >= 4 and <= 26 ? weeks.Value : 8;
        var start = DateTime.UtcNow.Date.AddDays(-WeeksCount * 7);
        Rows = await _svc.WeeklyCohortsAsync(start, WeeksCount);
    }
}
