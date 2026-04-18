using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Models.Forms;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Pages.Admin.Submissions;

public class IndexModel : PageModel
{
    private readonly IFormService _forms;

    public IndexModel(IFormService forms)
    {
        _forms = forms;
    }

    public const int PageSizeConst = 25;
    public int PageSize => PageSizeConst;

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Role { get; set; }

    public List<FormSubmission> Submissions { get; private set; } = new();
    public int TotalCount { get; private set; }

    public async Task OnGetAsync()
    {
        if (Page < 1) Page = 1;
        Submissions = await _forms.GetSubmissionsAsync(Page, PageSizeConst, formType: null, role: string.IsNullOrWhiteSpace(Role) ? null : Role);
        TotalCount = await _forms.GetSubmissionCountAsync(formType: null, role: string.IsNullOrWhiteSpace(Role) ? null : Role);
    }
}
