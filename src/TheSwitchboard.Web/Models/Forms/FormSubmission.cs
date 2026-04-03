namespace TheSwitchboard.Web.Models.Forms;

public class FormSubmission
{
    public int Id { get; set; }
    public required string FormType { get; set; }
    public required string Data { get; set; }
    public string? SourcePage { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool SentToPhoenix { get; set; }
    public string? PhoenixResponse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
