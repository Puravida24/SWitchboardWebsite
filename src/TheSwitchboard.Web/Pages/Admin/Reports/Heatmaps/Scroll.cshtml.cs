using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports.Heatmaps;

public class ScrollModel : PageModel
{
    private readonly IEngagementAnalyticsService _svc;
    public ScrollModel(IEngagementAnalyticsService svc) { _svc = svc; }

    public string Path { get; private set; } = "/";
    public int WindowDays { get; private set; } = 30;
    public DateTime FromUtc { get; private set; }
    public DateTime ToUtc { get; private set; }
    public ScrollHeatmap Report { get; private set; } = new("/", 0, Array.Empty<ScrollBand>());
    public IReadOnlyList<string> AvailablePaths { get; private set; } = Array.Empty<string>();

    public async Task OnGetAsync(string? path, int? days)
    {
        Path = string.IsNullOrWhiteSpace(path) ? "/" : path!;
        WindowDays = days is >= 1 and <= 180 ? days.Value : 30;
        ToUtc = DateTime.UtcNow;
        FromUtc = ToUtc.AddDays(-WindowDays);

        AvailablePaths = await _svc.DistinctScrollPathsAsync(FromUtc, ToUtc);
        Report = await _svc.ScrollAsync(Path, FromUtc, ToUtc);
    }
}
