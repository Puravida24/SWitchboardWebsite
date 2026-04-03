namespace TheSwitchboard.Web.Models.Email;

public class Subscriber
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? Industry { get; set; }
    public bool IsActive { get; set; } = true;
    public string? UnsubscribeToken { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnsubscribedAt { get; set; }
}
