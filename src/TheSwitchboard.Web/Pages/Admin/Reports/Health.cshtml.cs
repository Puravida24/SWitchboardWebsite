using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TheSwitchboard.Web.Pages.Admin.Reports;

public class HealthModel : PageModel
{
    /// <summary>Resolved build id used to cache-bust tracker.js (best-effort).</summary>
    public string BuildId { get; private set; } = "dev";

    public void OnGet()
    {
        BuildId = Environment.GetEnvironmentVariable("RAILWAY_GIT_COMMIT_SHA")?[..Math.Min(8, Environment.GetEnvironmentVariable("RAILWAY_GIT_COMMIT_SHA")?.Length ?? 0)]
                  ?? "dev";
    }
}
