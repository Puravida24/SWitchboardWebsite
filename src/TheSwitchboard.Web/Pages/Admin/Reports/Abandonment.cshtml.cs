using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class AbandonmentModel : PageModel
{
    private readonly IEngagementAnalyticsService _svc;
    public AbandonmentModel(IEngagementAnalyticsService svc) { _svc = svc; }

    public int WindowDays { get; private set; } = 30;
    public DateTime FromUtc { get; private set; }
    public DateTime ToUtc { get; private set; }
    public AbandonmentReport Report { get; private set; } = new(0, 0, Array.Empty<AbandonmentRow>(), Array.Empty<ExitPathRow>());

    public async Task OnGetAsync(int? days)
    {
        WindowDays = days is >= 1 and <= 180 ? days.Value : 30;
        ToUtc = DateTime.UtcNow;
        FromUtc = ToUtc.AddDays(-WindowDays);
        Report = await _svc.AbandonmentAsync(FromUtc, ToUtc);
    }
}
