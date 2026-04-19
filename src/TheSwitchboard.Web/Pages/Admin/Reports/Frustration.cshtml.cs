using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class FrustrationModel : PageModel
{
    private readonly IFrustrationAnalyticsService _service;

    public FrustrationModel(IFrustrationAnalyticsService service) { _service = service; }

    public int WindowDays { get; private set; } = 30;
    public DateTime FromUtc { get; private set; }
    public DateTime ToUtc { get; private set; }

    public FrustrationSummary Summary { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<FrustrationRow> Rage { get; private set; } = Array.Empty<FrustrationRow>();
    public IReadOnlyList<FrustrationRow> Dead { get; private set; } = Array.Empty<FrustrationRow>();
    public IReadOnlyList<FrustratedPage> TopPages { get; private set; } = Array.Empty<FrustratedPage>();

    public async Task OnGetAsync(int? days)
    {
        WindowDays = days is >= 1 and <= 180 ? days.Value : 30;
        ToUtc = DateTime.UtcNow;
        FromUtc = ToUtc.AddDays(-WindowDays);

        Summary = await _service.SummaryAsync(FromUtc, ToUtc);
        Rage = await _service.RageClicksAsync(FromUtc, ToUtc);
        Dead = await _service.DeadClicksAsync(FromUtc, ToUtc);
        TopPages = await _service.TopFrustratedPagesAsync(FromUtc, ToUtc);
    }
}
