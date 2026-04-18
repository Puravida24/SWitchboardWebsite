namespace TheSwitchboard.Web.Models.Site;

public class SiteSettings
{
    public int Id { get; set; }
    public required string SiteName { get; set; }
    public string? SiteTagline { get; set; }
    public string? ContactEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? FacebookUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? TwitterUrl { get; set; }

    // Slice 3 — editable homepage copy (locked design, flexible text)
    public string? HeroHeadline { get; set; }
    public string? HeroDeck { get; set; }
    public string? EditorialKicker { get; set; }
    public string? EditorialHeadline { get; set; }
    public string? EditorialDeck { get; set; }
    public string? EditorialBodyLeft { get; set; }
    public string? EditorialBodyRight { get; set; }
    public string? PullQuote { get; set; }
    public string? PullQuoteAttribution { get; set; }
    public string? CtaHeadline { get; set; }
    public string? CtaDeck { get; set; }
    public string? FooterCopyright { get; set; }
    public string? FooterStamp { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
