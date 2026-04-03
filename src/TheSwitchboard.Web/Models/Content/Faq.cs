namespace TheSwitchboard.Web.Models.Content;

public class Faq
{
    public int Id { get; set; }
    public required string Question { get; set; }
    public required string Answer { get; set; }
    public string? Category { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
