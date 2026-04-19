using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class ErrorsModel : PageModel
{
    private readonly IErrorImpactService _svc;
    public ErrorsModel(IErrorImpactService svc) { _svc = svc; }

    public int WindowDays { get; private set; } = 30;
    public DateTime FromUtc { get; private set; }
    public DateTime ToUtc { get; private set; }

    public IReadOnlyList<ErrorSummaryRow> Errors { get; private set; } = Array.Empty<ErrorSummaryRow>();

    public async Task OnGetAsync(int? days)
    {
        WindowDays = days is >= 1 and <= 180 ? days.Value : 30;
        ToUtc = DateTime.UtcNow;
        FromUtc = ToUtc.AddDays(-WindowDays);
        Errors = await _svc.ErrorListAsync(FromUtc, ToUtc);
    }
}
