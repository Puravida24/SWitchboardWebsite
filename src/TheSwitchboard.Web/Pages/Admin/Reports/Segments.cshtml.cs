using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services.Tracking;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class SegmentsModel : PageModel
{
    private readonly ISegmentService _svc;
    public SegmentsModel(ISegmentService svc) { _svc = svc; }

    public IReadOnlyList<Segment> Segments { get; private set; } = Array.Empty<Segment>();

    [BindProperty] public string NewName { get; set; } = "";
    [BindProperty] public string NewFilter { get; set; } = "{\"device\":\"desktop\"}";

    public async Task OnGetAsync()
    {
        Segments = await _svc.ListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) return RedirectToPage();
        await _svc.CreateAsync(NewName.Trim(), NewFilter, User.Identity?.Name);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id)
    {
        await _svc.DeleteAsync(id);
        return RedirectToPage();
    }
}
