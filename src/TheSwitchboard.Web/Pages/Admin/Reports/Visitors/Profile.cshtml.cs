using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports.Visitors;

public class ProfileModel : PageModel
{
    private readonly IVisitorAnalyticsService _svc;
    public ProfileModel(IVisitorAnalyticsService svc) { _svc = svc; }

    public VisitorProfile? Profile { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        Profile = await _svc.GetProfileAsync(id);
        if (Profile is null) return NotFound();
        return Page();
    }
}
