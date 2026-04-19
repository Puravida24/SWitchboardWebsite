using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class OverviewModel : PageModel
{
    private readonly IOverviewService _svc;
    public OverviewModel(IOverviewService svc) { _svc = svc; }

    public int WindowDays { get; private set; } = 7;
    public OverviewReport Report { get; private set; } =
        new(0, 0, 0, 0, 100, 0, Array.Empty<DailyBucket>(), Array.Empty<TopRow>(), Array.Empty<TopRow>(), Array.Empty<TopRow>());

    public async Task OnGetAsync(int? days)
    {
        WindowDays = days is >= 1 and <= 90 ? days.Value : 7;
        Report = await _svc.GetAsync(DateTime.UtcNow.AddDays(-WindowDays), DateTime.UtcNow);
    }
}
