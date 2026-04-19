using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class AttributionModel : PageModel
{
    private readonly IAttributionAnalyticsService _service;

    public AttributionModel(IAttributionAnalyticsService service)
    {
        _service = service;
    }

    public DateTime FromUtc { get; private set; }
    public DateTime ToUtc { get; private set; }
    public int WindowDays { get; private set; } = 30;

    public IReadOnlyList<AttributionBreakdown> Rows { get; private set; } = Array.Empty<AttributionBreakdown>();
    public AttributionSummary Summary { get; private set; } = new(0, 0, 0, 0, 0, 0);

    public string? PreviewUrl { get; private set; }
    public ParsedAttribution? Preview { get; private set; }

    public async Task OnGetAsync(int? days, string? url)
    {
        WindowDays = days is >= 1 and <= 180 ? days.Value : 30;
        ToUtc = DateTime.UtcNow;
        FromUtc = ToUtc.AddDays(-WindowDays);

        Rows = await _service.GetBreakdownAsync(FromUtc, ToUtc);
        Summary = await _service.GetSummaryAsync(FromUtc, ToUtc);

        if (!string.IsNullOrWhiteSpace(url))
        {
            PreviewUrl = url;
            Preview = _service.ParsePreview(url);
        }
    }
}
