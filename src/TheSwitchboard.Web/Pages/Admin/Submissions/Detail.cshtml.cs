using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Models.Forms;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Pages.Admin.Submissions;

public class DetailModel : PageModel
{
    private readonly IFormService _forms;

    public DetailModel(IFormService forms)
    {
        _forms = forms;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public FormSubmission? Submission { get; private set; }

    public async Task OnGetAsync()
    {
        if (Id <= 0) return;
        Submission = await _forms.GetSubmissionByIdAsync(Id);
    }
}
