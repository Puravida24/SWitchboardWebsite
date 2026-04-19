using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TheSwitchboard.Web.Pages.Admin;

public class ComingSoonModel : PageModel
{
    public string Feature { get; private set; } = "This section";
    public string? Eta { get; private set; }

    public void OnGet(string? feature, string? eta)
    {
        if (!string.IsNullOrWhiteSpace(feature)) Feature = feature;
        Eta = eta;
    }
}
