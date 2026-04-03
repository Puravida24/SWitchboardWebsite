namespace TheSwitchboard.Web.Models.Content;

public class Testimonial
{
    public int Id { get; set; }
    public required string Quote { get; set; }
    public required string PersonName { get; set; }
    public required string PersonTitle { get; set; }
    public required string CompanyName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
