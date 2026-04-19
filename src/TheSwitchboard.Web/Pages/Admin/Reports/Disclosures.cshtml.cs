using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class DisclosuresModel : PageModel
{
    private readonly IComplianceAnalyticsService _svc;
    public DisclosuresModel(IComplianceAnalyticsService svc) { _svc = svc; }

    public IReadOnlyList<DisclosureVersion> Versions { get; private set; } = Array.Empty<DisclosureVersion>();
    public IDictionary<long, int> CertCounts { get; private set; } = new Dictionary<long, int>();

    public async Task OnGetAsync()
    {
        Versions = await _svc.ListVersionsAsync();
        CertCounts = await _svc.CertCountByVersionAsync();
    }

    public async Task<IActionResult> OnPostRegisterAsync(long id)
    {
        await _svc.RegisterVersionAsync(id, User.Identity?.Name);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRetireAsync(long id)
    {
        await _svc.RetireVersionAsync(id, User.Identity?.Name);
        return RedirectToPage();
    }
}
