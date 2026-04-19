using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class TrendsModel : PageModel
{
    private readonly ITrendsService _svc;
    public TrendsModel(ITrendsService svc) { _svc = svc; }

    public string Metric { get; private set; } = "sessions";
    public int WindowDays { get; private set; } = 30;
    public TrendSeries Series { get; private set; } = new("sessions", Array.Empty<DateTime>(), Array.Empty<int>(), Array.Empty<int>(), 0, 0, 0);

    public static readonly string[] Metrics = { "sessions", "pageviews", "submissions", "errors", "conversions" };

    public async Task OnGetAsync(string? metric, int? days)
    {
        Metric = Metrics.Contains(metric ?? "") ? metric! : "sessions";
        WindowDays = days is >= 1 and <= 90 ? days.Value : 30;
        Series = await _svc.SeriesAsync(Metric, DateTime.UtcNow.AddDays(-WindowDays), DateTime.UtcNow);
    }
}
