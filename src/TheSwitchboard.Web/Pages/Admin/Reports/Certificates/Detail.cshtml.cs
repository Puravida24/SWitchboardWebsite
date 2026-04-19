using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports.Certificates;

public class DetailModel : PageModel
{
    private readonly IComplianceAnalyticsService _svc;
    public DetailModel(IComplianceAnalyticsService svc) { _svc = svc; }

    public ConsentCertificate? Cert { get; private set; }
    public bool IsExpired => Cert is not null && Cert.ExpiresAt < DateTime.UtcNow;

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        Cert = await _svc.GetCertAsync(id);
        if (Cert is null) return NotFound();
        return Page();
    }
}
