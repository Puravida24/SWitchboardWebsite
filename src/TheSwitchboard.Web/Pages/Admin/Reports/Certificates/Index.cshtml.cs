using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports.Certificates;

public class IndexModel : PageModel
{
    private readonly IComplianceAnalyticsService _svc;
    public IndexModel(IComplianceAnalyticsService svc) { _svc = svc; }

    public const int PageSize = 50;
    public int WindowDays { get; private set; } = 30;
    public int PageIndex { get; private set; } = 1;
    public string? Status { get; private set; }
    public DateTime FromUtc { get; private set; }
    public DateTime ToUtc { get; private set; }
    public IReadOnlyList<ConsentCertificate> Rows { get; private set; } = Array.Empty<ConsentCertificate>();

    public async Task OnGetAsync(int? days, int? page, string? status)
    {
        WindowDays = days is >= 1 and <= 365 ? days.Value : 30;
        PageIndex = page is >= 1 ? page.Value : 1;
        Status = status;
        ToUtc = DateTime.UtcNow;
        FromUtc = ToUtc.AddDays(-WindowDays);
        Rows = await _svc.ListCertsAsync(FromUtc, ToUtc, Status, (PageIndex - 1) * PageSize, PageSize);
    }
}
