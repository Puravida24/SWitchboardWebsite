using Microsoft.AspNetCore.Mvc.RazorPages;
using TheSwitchboard.Web.Models.Forms;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly IAnalyticsService _analytics;
    private readonly IFormService _formService;
    private readonly IContentService _contentService;

    public DashboardModel(IAnalyticsService analytics, IFormService formService, IContentService contentService)
    {
        _analytics = analytics;
        _formService = formService;
        _contentService = contentService;
    }

    public int PageViewsToday { get; set; }
    public int PageViewsWeek { get; set; }
    public int TotalSubmissions { get; set; }
    public int BlogPostCount { get; set; }
    public List<FormSubmission> RecentSubmissions { get; set; } = [];

    public async Task OnGetAsync()
    {
        var now = DateTime.UtcNow;
        PageViewsToday = await _analytics.GetPageViewCountAsync(now.Date, now);
        PageViewsWeek = await _analytics.GetPageViewCountAsync(now.Date.AddDays(-7), now);
        TotalSubmissions = await _formService.GetSubmissionCountAsync();
        BlogPostCount = await _contentService.GetBlogPostCountAsync();
        RecentSubmissions = await _formService.GetSubmissionsAsync(1, 10);
    }
}
