using System.Net;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Site;

namespace TheSwitchboard.Web.Pages;

/// <summary>
/// Shared base for public pages that render static HTML (ported from /wireframes)
/// with runtime token substitution for dynamic SiteSettings values.
///
/// Tokens supported in the source HTML:
///   {{PHONE}}   → SiteSettings.PhoneNumber
///   {{EMAIL}}   → SiteSettings.ContactEmail
///   {{ADDRESS}} → SiteSettings.Address
///   {{SITE_NAME}} → SiteSettings.SiteName
///
/// Each concrete page sets <see cref="SourceFile"/> (relative to wwwroot) in its
/// override of OnGetAsync before calling base.OnGetAsync().
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

    /// <summary>Path under wwwroot/ to the source HTML (e.g. "wireframes/design-32e-newsprint.html").</summary>
    protected abstract string SourceFile { get; }

    /// <summary>Rendered HTML with tokens substituted. Populated by OnGetAsync.</summary>
    public string Html { get; private set; } = string.Empty;

    public virtual async Task OnGetAsync()
    {
        var path = Path.Combine(_env.WebRootPath, SourceFile);
        var html = await System.IO.File.ReadAllTextAsync(path);
        var settings = await _db.Set<SiteSettings>().FirstOrDefaultAsync()
                       ?? new SiteSettings { SiteName = "The Switchboard" };

        // SiteSettings values are admin-editable. HTML-encode them on substitution to
        // prevent stored-XSS: an admin with weak OpSec (or a successful credential-stuffing
        // attack against the admin account) could otherwise inject arbitrary markup into
        // every public page at once by saving "<script>…</script>" as the phone number.
        html = html
            .Replace("{{PHONE}}",     WebUtility.HtmlEncode(settings.PhoneNumber ?? string.Empty))
            .Replace("{{EMAIL}}",     WebUtility.HtmlEncode(settings.ContactEmail ?? string.Empty))
            .Replace("{{ADDRESS}}",   WebUtility.HtmlEncode(settings.Address ?? string.Empty))
            .Replace("{{SITE_NAME}}", WebUtility.HtmlEncode(settings.SiteName));

        Html = html;
    }
}
