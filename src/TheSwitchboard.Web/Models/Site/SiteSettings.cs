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
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
