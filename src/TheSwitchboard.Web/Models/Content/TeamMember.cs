namespace TheSwitchboard.Web.Models.Content;

public class TeamMember
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Title { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
