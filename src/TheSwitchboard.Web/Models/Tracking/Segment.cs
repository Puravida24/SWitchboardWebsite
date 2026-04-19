using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

public class Segment
{
    public long Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(2000)] public string Filter { get; set; } = "{}";
    [MaxLength(120)] public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
