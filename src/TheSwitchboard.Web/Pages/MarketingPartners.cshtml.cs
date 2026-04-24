using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;

namespace TheSwitchboard.Web.Pages;

public class MarketingPartnersModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _db;

    public MarketingPartnersModel(IWebHostEnvironment env, AppDbContext db)
    {
        _env = env;
        _db = db;
    }

    public string Html { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var rows = await _db.MarketingPartners
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Name, p.WebsiteUrl })
            .ToListAsync();

        var templatePath = Path.Combine(_env.WebRootPath, "wireframes", "marketing-partners.html");
        var html = System.IO.File.Exists(templatePath)
            ? await System.IO.File.ReadAllTextAsync(templatePath)
            : "<!doctype html><html><body><h1>Marketing Partners</h1>{{PARTNERS_GRID}}</body></html>";

        string gridHtml;
        if (rows.Count == 0)
        {
            gridHtml = "<p class=\"empty-state\">No marketing partners are currently listed.</p>";
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"partners-grid\">");
            foreach (var r in rows)
            {
                sb.Append("<div class=\"partner-item\">");
                if (!string.IsNullOrWhiteSpace(r.WebsiteUrl))
                {
                    sb.Append("<a href=\"")
                      .Append(WebUtility.HtmlEncode(r.WebsiteUrl))
                      .Append("\" target=\"_blank\" rel=\"noopener\">")
                      .Append(WebUtility.HtmlEncode(r.Name))
                      .Append("</a>");
                }
                else
                {
                    sb.Append(WebUtility.HtmlEncode(r.Name));
                }
                sb.Append("</div>");
            }
            sb.Append("</div>");
            gridHtml = sb.ToString();
        }

        html = html
            .Replace("{{PARTNERS_GRID}}", gridHtml)
            .Replace("{{PARTNERS_COUNT}}", rows.Count.ToString("N0"))
            .Replace("{{LAST_UPDATED}}", DateTime.UtcNow.ToString("MMMM d, yyyy"));

        Html = html;
    }
}
