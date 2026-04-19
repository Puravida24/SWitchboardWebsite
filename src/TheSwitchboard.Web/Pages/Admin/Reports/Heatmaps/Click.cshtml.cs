using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports.Heatmaps;

public class ClickModel : PageModel
{
    private readonly IFrustrationAnalyticsService _service;

    public ClickModel(IFrustrationAnalyticsService service) { _service = service; }

    public string Path { get; private set; } = "/";
    public int WindowDays { get; private set; } = 30;
    public DateTime FromUtc { get; private set; }
    public DateTime ToUtc { get; private set; }

    public IReadOnlyList<ClickDot> Dots { get; private set; } = Array.Empty<ClickDot>();
    public IReadOnlyList<string> AvailablePaths { get; private set; } = Array.Empty<string>();
    public int RageCount { get; private set; }
    public int DeadCount { get; private set; }

    public async Task OnGetAsync(string? path, int? days, string? filter)
    {
        Path = string.IsNullOrWhiteSpace(path) ? "/" : path!;
        WindowDays = days is >= 1 and <= 180 ? days.Value : 30;
        ToUtc = DateTime.UtcNow;
        FromUtc = ToUtc.AddDays(-WindowDays);
        Filter = filter;

        AvailablePaths = await _service.DistinctPathsAsync(FromUtc, ToUtc);
        Dots = await _service.ClickDotsForPathAsync(Path, FromUtc, ToUtc);
        RageCount = Dots.Count(d => d.IsRage);
        DeadCount = Dots.Count(d => d.IsDead);
    }

    public string? Filter { get; private set; }
}
