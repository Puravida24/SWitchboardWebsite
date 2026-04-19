using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports.Forms;

public class FunnelModel : PageModel
{
    private readonly IEngagementAnalyticsService _svc;
    public FunnelModel(IEngagementAnalyticsService svc) { _svc = svc; }

    public string FormId { get; private set; } = "contact";
    public int WindowDays { get; private set; } = 30;
    public DateTime FromUtc { get; private set; }
    public DateTime ToUtc { get; private set; }

    public FunnelReport Report { get; private set; } = new("contact", 0, 0, 0, Array.Empty<FunnelStep>());
    public IReadOnlyList<string> AvailableForms { get; private set; } = Array.Empty<string>();

    public async Task OnGetAsync(string? formId, int? days)
    {
        FormId = string.IsNullOrWhiteSpace(formId) ? "contact" : formId!;
        WindowDays = days is >= 1 and <= 180 ? days.Value : 30;
        ToUtc = DateTime.UtcNow;
        FromUtc = ToUtc.AddDays(-WindowDays);

        AvailableForms = await _svc.DistinctFormsAsync(FromUtc, ToUtc);
        Report = await _svc.FunnelAsync(FormId, FromUtc, ToUtc);
    }
}
