namespace TheSwitchboard.Web.Models.Content;

public class ClientLogo
{
    public int Id { get; set; }
    public required string CompanyName { get; set; }
    public required string LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
