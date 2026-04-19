using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports.Visitors;

public class IndexModel : PageModel
{
    private readonly IVisitorAnalyticsService _svc;
    public IndexModel(IVisitorAnalyticsService svc) { _svc = svc; }

    public const int PageSize = 50;
    public int WindowDays { get; private set; } = 30;
    public int PageIndex { get; private set; } = 1;
    public int Total { get; private set; }
    public IReadOnlyList<VisitorRow> Rows { get; private set; } = Array.Empty<VisitorRow>();

    public async Task OnGetAsync(int? days, int? page)
    {
        WindowDays = days is >= 1 and <= 365 ? days.Value : 30;
        PageIndex = page is >= 1 ? page.Value : 1;
        var fromUtc = DateTime.UtcNow.AddDays(-WindowDays);
        Total = await _svc.CountAsync(fromUtc, DateTime.UtcNow);
        Rows = await _svc.ListAsync(fromUtc, DateTime.UtcNow, (PageIndex - 1) * PageSize, PageSize);
    }
}
