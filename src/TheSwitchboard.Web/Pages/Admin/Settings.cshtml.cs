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

        settings.SiteName     = Input.SiteName;
        settings.SiteTagline  = Input.SiteTagline;
        settings.ContactEmail = Input.ContactEmail;
        settings.PhoneNumber  = Input.PhoneNumber;
        settings.Address      = Input.Address;
        settings.FacebookUrl  = Input.FacebookUrl;
        settings.LinkedInUrl  = Input.LinkedInUrl;
        settings.TwitterUrl   = Input.TwitterUrl;
        settings.UpdatedAt    = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        StatusMessage = "Saved.";
        return Page();
    }

    public class SettingsInput
    {
        [Required, StringLength(120)]
        public string SiteName { get; set; } = "The Switchboard";

        [StringLength(200)]
        public string? SiteTagline { get; set; }

        [EmailAddress, StringLength(200)]
        public string? ContactEmail { get; set; }

        [StringLength(64)]
        public string? PhoneNumber { get; set; }

        [StringLength(400)]
        public string? Address { get; set; }

        [Url, StringLength(400)]
        public string? FacebookUrl { get; set; }

        [Url, StringLength(400)]
        public string? LinkedInUrl { get; set; }

        [Url, StringLength(400)]
        public string? TwitterUrl { get; set; }

        public static SettingsInput FromEntity(SiteSettings s) => new()
        {
            SiteName     = s.SiteName,
            SiteTagline  = s.SiteTagline,
            ContactEmail = s.ContactEmail,
            PhoneNumber  = s.PhoneNumber,
            Address      = s.Address,
            FacebookUrl  = s.FacebookUrl,
            LinkedInUrl  = s.LinkedInUrl,
            TwitterUrl   = s.TwitterUrl,
        };
    }
}
