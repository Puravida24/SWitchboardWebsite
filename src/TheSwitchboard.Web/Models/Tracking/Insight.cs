using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Auto-generated observation from <c>InsightsService</c>. Higher |Score| = more
/// surprising (z-score). Severity bucket: info | warn | critical.
/// </summary>
public class Insight
{
    public long Id { get; set; }

    [Required, MaxLength(500)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Metric { get; set; } = string.Empty;
    [MaxLength(500)]           public string? Path { get; set; }
    public double Score { get; set; }
    public double Current { get; set; }
    public double Baseline { get; set; }
    [MaxLength(20)] public string Severity { get; set; } = "info";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DismissedAt { get; set; }
}
