using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Site;

namespace TheSwitchboard.Web.Pages.Admin;

public class SettingsModel : PageModel
{
    private readonly AppDbContext _db;

    public SettingsModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public SettingsInput Input { get; set; } = new();

    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var settings = await _db.Set<SiteSettings>().FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new SiteSettings { SiteName = "The Switchboard" };
            _db.Add(settings);
            await _db.SaveChangesAsync();
        }
        Input = SettingsInput.FromEntity(settings);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var settings = await _db.Set<SiteSettings>().FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new SiteSettings { SiteName = Input.SiteName };
            _db.Add(settings);
        }

        // Slice 3 brand-rule guard: reject saves containing "lead"/"leads" in body text.
        // Enforce at write-time so the rule can't be bypassed by anyone with admin access.
        var leadRe = new System.Text.RegularExpressions.Regex(@"\blead(s)?\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var textBundle = string.Join(" ",
            Input.SiteName, Input.SiteTagline, Input.HeroHeadline, Input.HeroDeck,
            Input.EditorialKicker, Input.EditorialHeadline, Input.EditorialDeck,
            Input.EditorialBodyLeft, Input.EditorialBodyRight,
            Input.PullQuote, Input.PullQuoteAttribution,
            Input.CtaHeadline, Input.CtaDeck,
            Input.FooterCopyright, Input.FooterStamp);
        if (leadRe.IsMatch(textBundle))
        {
            ModelState.AddModelError(string.Empty, "Brand rule: 'lead' / 'leads' is not allowed in customer-facing copy. Use 'prospects', 'opportunities', or 'records'.");
            return Page();
        }

        // Version-history: snapshot each changed field before mutating.
        void Snapshot(string field, string? oldV, string? newV)
        {
            if (oldV == newV) return;
            _db.ContentVersions.Add(new Models.Content.ContentVersion
            {
                EntityType = "sitesettings",
                EntityKey = settings.Id.ToString(),
                FieldName = field,
                OldValue = oldV,
                NewValue = newV,
                UpdatedBy = User?.Identity?.Name,
            });
        }
        Snapshot(nameof(SiteSettings.SiteName),             settings.SiteName,             Input.SiteName);
        Snapshot(nameof(SiteSettings.SiteTagline),          settings.SiteTagline,          Input.SiteTagline);
        Snapshot(nameof(SiteSettings.ContactEmail),         settings.ContactEmail,         Input.ContactEmail);
        Snapshot(nameof(SiteSettings.PhoneNumber),          settings.PhoneNumber,          Input.PhoneNumber);
        Snapshot(nameof(SiteSettings.Address),              settings.Address,              Input.Address);
        Snapshot(nameof(SiteSettings.HeroHeadline),         settings.HeroHeadline,         Input.HeroHeadline);
        Snapshot(nameof(SiteSettings.HeroDeck),             settings.HeroDeck,             Input.HeroDeck);
        Snapshot(nameof(SiteSettings.EditorialKicker),      settings.EditorialKicker,      Input.EditorialKicker);
        Snapshot(nameof(SiteSettings.EditorialHeadline),    settings.EditorialHeadline,    Input.EditorialHeadline);
        Snapshot(nameof(SiteSettings.EditorialDeck),        settings.EditorialDeck,        Input.EditorialDeck);
        Snapshot(nameof(SiteSettings.EditorialBodyLeft),    settings.EditorialBodyLeft,    Input.EditorialBodyLeft);
        Snapshot(nameof(SiteSettings.EditorialBodyRight),   settings.EditorialBodyRight,   Input.EditorialBodyRight);
        Snapshot(nameof(SiteSettings.PullQuote),            settings.PullQuote,            Input.PullQuote);
        Snapshot(nameof(SiteSettings.PullQuoteAttribution), settings.PullQuoteAttribution, Input.PullQuoteAttribution);
        Snapshot(nameof(SiteSettings.CtaHeadline),          settings.CtaHeadline,          Input.CtaHeadline);
        Snapshot(nameof(SiteSettings.CtaDeck),              settings.CtaDeck,              Input.CtaDeck);

        settings.SiteName              = Input.SiteName;
        settings.SiteTagline           = Input.SiteTagline;
        settings.ContactEmail          = Input.ContactEmail;
        settings.PhoneNumber           = Input.PhoneNumber;
        settings.Address               = Input.Address;
        settings.FacebookUrl           = Input.FacebookUrl;
        settings.LinkedInUrl           = Input.LinkedInUrl;
        settings.TwitterUrl            = Input.TwitterUrl;
        settings.HeroHeadline          = Input.HeroHeadline;
        settings.HeroDeck              = Input.HeroDeck;
        settings.EditorialKicker       = Input.EditorialKicker;
        settings.EditorialHeadline     = Input.EditorialHeadline;
        settings.EditorialDeck         = Input.EditorialDeck;
        settings.EditorialBodyLeft     = Input.EditorialBodyLeft;
        settings.EditorialBodyRight    = Input.EditorialBodyRight;
        settings.PullQuote             = Input.PullQuote;
        settings.PullQuoteAttribution  = Input.PullQuoteAttribution;
        settings.CtaHeadline           = Input.CtaHeadline;
        settings.CtaDeck               = Input.CtaDeck;
        settings.FooterCopyright       = Input.FooterCopyright;
        settings.FooterStamp           = Input.FooterStamp;
        settings.UpdatedAt             = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        StatusMessage = "Saved.";
        return Page();
    }

    public class SettingsInput
    {
        [Required, StringLength(120)]
        public string SiteName { get; set; } = "The Switchboard";

        [StringLength(200)] public string? SiteTagline { get; set; }
        [EmailAddress, StringLength(200)] public string? ContactEmail { get; set; }
        [StringLength(64)] public string? PhoneNumber { get; set; }
        [StringLength(400)] public string? Address { get; set; }
        [Url, StringLength(400)] public string? FacebookUrl { get; set; }
        [Url, StringLength(400)] public string? LinkedInUrl { get; set; }
        [Url, StringLength(400)] public string? TwitterUrl { get; set; }

        // Slice 3 copy fields
        [StringLength(400)] public string? HeroHeadline { get; set; }
        [StringLength(1000)] public string? HeroDeck { get; set; }
        [StringLength(120)] public string? EditorialKicker { get; set; }
        [StringLength(400)] public string? EditorialHeadline { get; set; }
        [StringLength(1000)] public string? EditorialDeck { get; set; }
        [StringLength(4000)] public string? EditorialBodyLeft { get; set; }
        [StringLength(4000)] public string? EditorialBodyRight { get; set; }
        [StringLength(1000)] public string? PullQuote { get; set; }
        [StringLength(200)] public string? PullQuoteAttribution { get; set; }
        [StringLength(400)] public string? CtaHeadline { get; set; }
        [StringLength(1000)] public string? CtaDeck { get; set; }
        [StringLength(400)] public string? FooterCopyright { get; set; }
        [StringLength(400)] public string? FooterStamp { get; set; }

        public static SettingsInput FromEntity(SiteSettings s) => new()
        {
            SiteName             = s.SiteName,
            SiteTagline          = s.SiteTagline,
            ContactEmail         = s.ContactEmail,
            PhoneNumber          = s.PhoneNumber,
            Address              = s.Address,
            FacebookUrl          = s.FacebookUrl,
            LinkedInUrl          = s.LinkedInUrl,
            TwitterUrl           = s.TwitterUrl,
            HeroHeadline         = s.HeroHeadline,
            HeroDeck             = s.HeroDeck,
            EditorialKicker      = s.EditorialKicker,
            EditorialHeadline    = s.EditorialHeadline,
            EditorialDeck        = s.EditorialDeck,
            EditorialBodyLeft    = s.EditorialBodyLeft,
            EditorialBodyRight   = s.EditorialBodyRight,
            PullQuote            = s.PullQuote,
            PullQuoteAttribution = s.PullQuoteAttribution,
            CtaHeadline          = s.CtaHeadline,
            CtaDeck              = s.CtaDeck,
            FooterCopyright      = s.FooterCopyright,
            FooterStamp          = s.FooterStamp,
        };
    }
}
