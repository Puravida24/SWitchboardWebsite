using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Pages;

/// <summary>
/// Public consent-verification page — /verify/{certificateId}.
///
/// Renders the disclosure text + capture environment + WCAG chip + timestamp.
/// Explicitly hides IP / email hash / phone hash / replay so the link is
/// shareable without leaking PII.
/// </summary>
public class VerifyModel : PageModel
{
    private readonly AppDbContext _db;
    public VerifyModel(AppDbContext db) { _db = db; }

    public ConsentCertificate? Cert { get; private set; }
    public bool IsExpired => Cert is not null && Cert.ExpiresAt < DateTime.UtcNow;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        Cert = await _db.ConsentCertificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CertificateId == id);
        if (Cert is null) return NotFound();
        if (IsExpired) return StatusCode(StatusCodes.Status410Gone);
        return Page();
    }
}
