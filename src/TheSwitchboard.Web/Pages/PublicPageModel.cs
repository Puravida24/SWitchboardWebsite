using System.Net;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Content;
using TheSwitchboard.Web.Models.Site;

namespace TheSwitchboard.Web.Pages;

/// <summary>
/// Shared base for public pages. Loads either (a) a named LegalPage row from the DB
/// (for /privacy /terms /accessibility) or (b) a static HTML file from wwwroot (for
/// /) and performs SiteSettings token substitution + roster macro expansion.
///
/// Supported tokens (HTML-encoded on substitution to prevent stored XSS):
///   {{PHONE}} {{EMAIL}} {{ADDRESS}} {{SITE_NAME}}
///   {{HERO_HEADLINE}} {{HERO_DECK}}
///   {{EDITORIAL_KICKER}} {{EDITORIAL_HEADLINE}} {{EDITORIAL_DECK}}
///   {{EDITORIAL_BODY_LEFT}} {{EDITORIAL_BODY_RIGHT}}
///   {{PULL_QUOTE}} {{PULL_QUOTE_ATTRIB}}
///   {{CTA_HEADLINE}} {{CTA_DECK}}
///   {{FOOTER_COPYRIGHT}} {{FOOTER_STAMP}}
///
/// Macros (raw, not encoded — content is rendered by trusted server code):
///   {{ROSTER}} — injects the ecosystem carousel from active ClientLogo rows.
///
/// Concrete pages override <see cref="SourceFile"/> for file-backed rendering, or
/// <see cref="LegalSlug"/> for DB-backed legal pages (which is XSS-sanitized on read).
/// </summary>
public abstract class PublicPageModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _db;

    protected PublicPageModel(IWebHostEnvironment env, AppDbContext db)
    {
        _env = env;
        _db = db;
    }

    /// <summary>Path under wwwroot/ to the source HTML. Used when <see cref="LegalSlug"/> is null.</summary>
    protected virtual string SourceFile => string.Empty;

    /// <summary>If set, the page reads its content from LegalPage WHERE Slug = this.</summary>
    protected virtual string? LegalSlug => null;

    public string Html { get; private set; } = string.Empty;

    public virtual async Task OnGetAsync()
    {
        string html;
        if (!string.IsNullOrEmpty(LegalSlug))
        {
            // Pull from DB; sanitize on render so any stored HTML can't inject scripts.
            var row = await _db.Set<LegalPage>().FirstOrDefaultAsync(p => p.Slug == LegalSlug);
            if (row is not null)
            {
                var sanitizer = new HtmlSanitizer();
                html = sanitizer.Sanitize(row.HtmlContent);
            }
            else
            {
                // Fallback to the wireframe HTML on first boot / empty DB.
                var fallback = Path.Combine(_env.WebRootPath, "wireframes", $"{LegalSlug}.html");
                html = System.IO.File.Exists(fallback)
                    ? await System.IO.File.ReadAllTextAsync(fallback)
                    : $"<!doctype html><html><body><h1>{WebUtility.HtmlEncode(LegalSlug)}</h1></body></html>";
            }
        }
        else
        {
            var path = Path.Combine(_env.WebRootPath, SourceFile);
            html = await System.IO.File.ReadAllTextAsync(path);
        }

        var settings = await _db.Set<SiteSettings>().FirstOrDefaultAsync()
                       ?? new SiteSettings { SiteName = "The Switchboard" };

        string E(string? v, string fallback = "") => WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(v) ? fallback : v!);

        // Per-request CSP nonce for inline <script nonce="{{NONCE}}"> blocks.
        var nonce = TheSwitchboard.Web.Middleware.CspNonceMiddleware.GetNonce(HttpContext);
        html = html.Replace("{{NONCE}}", nonce);

        html = html
            .Replace("{{PHONE}}",            E(settings.PhoneNumber))
            .Replace("{{EMAIL}}",            E(settings.ContactEmail))
            .Replace("{{ADDRESS}}",          E(settings.Address))
            .Replace("{{SITE_NAME}}",        E(settings.SiteName))
            .Replace("{{HERO_HEADLINE}}",    E(settings.HeroHeadline, "The precision layer for profitable insurance growth."))
            .Replace("{{HERO_DECK}}",        E(settings.HeroDeck, "Built by people who have written auto, home, and commercial policies. And paid for the demand that feeds them. Engineered for real-time decisions."))
            .Replace("{{EDITORIAL_KICKER}}", E(settings.EditorialKicker, "Editorial · Vol. I"))
            .Replace("{{EDITORIAL_HEADLINE}}", E(settings.EditorialHeadline, "Insurance is not a data problem."))
            .Replace("{{EDITORIAL_DECK}}",   E(settings.EditorialDeck, "It is a judgment business — and the gap between marketing and underwriting has always been the most expensive seat in the house."))
            .Replace("{{EDITORIAL_BODY_LEFT}}",  E(settings.EditorialBodyLeft))
            .Replace("{{EDITORIAL_BODY_RIGHT}}", E(settings.EditorialBodyRight))
            .Replace("{{PULL_QUOTE}}",       E(settings.PullQuote))
            .Replace("{{PULL_QUOTE_ATTRIB}}",E(settings.PullQuoteAttribution))
            .Replace("{{CTA_HEADLINE}}",     E(settings.CtaHeadline))
            .Replace("{{CTA_DECK}}",         E(settings.CtaDeck))
            .Replace("{{FOOTER_COPYRIGHT}}", E(settings.FooterCopyright))
            .Replace("{{FOOTER_STAMP}}",     E(settings.FooterStamp));

        if (html.Contains("{{ROSTER}}", StringComparison.Ordinal))
        {
            var partners = await _db.Set<ClientLogo>()
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => p.CompanyName)
                .ToListAsync();
            // Fallback: if DB is empty, use the roster that shipped with design-32e so the
            // carousel isn't blank on first-run.
            if (partners.Count == 0)
            {
                partners = new()
                {
                    "RightSure", "Root Insurance", "Florida One", "Progressive Commercial",
                    "American Family", "Next Insurance", "Next Call Club", "Aspire General"
                };
            }
            var sb = new System.Text.StringBuilder();
            foreach (var name in partners.Concat(partners))
            {
                sb.Append("<div class=\"ecosystem-card\"><span class=\"ecosystem-wordmark\">")
                  .Append(WebUtility.HtmlEncode(name))
                  .Append("</span></div>");
            }
            html = html.Replace("{{ROSTER}}", sb.ToString());
        }

        Html = html;
    }
}
