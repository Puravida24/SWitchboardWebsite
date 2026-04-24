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

    public async Task OnGetAsync(string? letter = null, string? q = null)
    {
        // Determine which letters have entries (case-insensitive first-char bucket).
        // EF Core can't group by Substring(Name, 1, 1) reliably cross-provider, so
        // pull distinct starting chars via a projected query.
        var firstCharsRaw = await _db.MarketingPartners
            .Where(p => p.IsActive && p.Name.Length > 0)
            .Select(p => p.Name.Substring(0, 1))
            .Distinct()
            .ToListAsync();
        var activeLetters = new HashSet<char>(
            firstCharsRaw
                .Select(s => s.Length > 0 ? char.ToUpperInvariant(s[0]) : ' ')
                .Where(c => c >= 'A' && c <= 'Z'));

        // Build the filtered list.
        var query = _db.MarketingPartners.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(letter) && letter.Length == 1)
        {
            var L = char.ToUpperInvariant(letter[0]);
            if (L >= 'A' && L <= 'Z')
            {
                var lower = char.ToLowerInvariant(L).ToString();
                var upper = L.ToString();
                query = query.Where(p => p.Name.StartsWith(upper) || p.Name.StartsWith(lower));
            }
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim();
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{needle}%"));
        }

        var rows = await query
            .OrderBy(p => p.Name)
            .Select(p => new { p.Name, p.WebsiteUrl })
            .ToListAsync();

        var templatePath = Path.Combine(_env.WebRootPath, "wireframes", "marketing-partners.html");
        var html = System.IO.File.Exists(templatePath)
            ? await System.IO.File.ReadAllTextAsync(templatePath)
            : "<!doctype html><html><body><h1>Marketing Partners</h1>{{AZ_NAV}}{{PARTNERS_GRID}}</body></html>";

        // ── A-Z nav strip ────────────────────────────────────────────
        var azSb = new StringBuilder();
        azSb.Append("<nav class=\"az-nav\" aria-label=\"Jump to letter\">");
        for (var c = 'A'; c <= 'Z'; c++)
        {
            var enabled = activeLetters.Contains(c);
            if (enabled)
            {
                var qs = string.IsNullOrWhiteSpace(q)
                    ? $"?letter={c}"
                    : $"?letter={c}&q={WebUtility.UrlEncode(q)}";
                azSb.Append("<a class=\"az-nav-link")
                    .Append(letter != null && letter.Length == 1 && char.ToUpperInvariant(letter[0]) == c ? " az-nav-active\"" : "\"")
                    .Append(" href=\"").Append(qs).Append("\">")
                    .Append(c).Append("</a>");
            }
            else
            {
                azSb.Append("<span class=\"az-nav-disabled\">").Append(c).Append("</span>");
            }
        }
        // "All" link to clear filters
        if (!string.IsNullOrEmpty(letter) || !string.IsNullOrWhiteSpace(q))
        {
            azSb.Append("<a class=\"az-nav-link az-nav-all\" href=\"/marketing-partners\">All</a>");
        }
        azSb.Append("</nav>");

        // ── Search form ───────────────────────────────────────────────
        var searchSb = new StringBuilder();
        searchSb.Append("<form class=\"partners-search\" method=\"get\" action=\"/marketing-partners\">");
        searchSb.Append("<input type=\"search\" name=\"q\" value=\"").Append(WebUtility.HtmlEncode(q ?? "")).Append("\" placeholder=\"Search partners…\" aria-label=\"Search partners\" />");
        searchSb.Append("<button type=\"submit\">Search</button>");
        searchSb.Append("</form>");

        // ── Grid ─────────────────────────────────────────────────────
        string gridHtml;
        if (rows.Count == 0)
        {
            gridHtml = "<p class=\"empty-state\">No partners match your filter.</p>";
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
            .Replace("{{AZ_NAV}}", azSb.ToString())
            .Replace("{{PARTNERS_SEARCH}}", searchSb.ToString())
            .Replace("{{PARTNERS_GRID}}", gridHtml)
            .Replace("{{PARTNERS_COUNT}}", rows.Count.ToString("N0"))
            .Replace("{{LAST_UPDATED}}", DateTime.UtcNow.ToString("MMMM d, yyyy"));

        Html = html;
    }
}
