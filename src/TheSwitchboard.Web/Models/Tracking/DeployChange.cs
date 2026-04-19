using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Human-readable deploy annotation — lets admins correlate a metric cliff to
/// a specific commit on the trend chart. Posted by CI after a successful
/// Railway deploy via POST /api/ops/deploy-change.
/// </summary>
public class DeployChange
{
    public long Id { get; set; }

    [Required, MaxLength(40)]
    public string Sha { get; set; } = string.Empty;

    public DateTime DeployedAt { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    /// <summary>bugfix | feature | tracking | infra</summary>
    [MaxLength(20)]
    public string? Category { get; set; }

    [MaxLength(120)]
    public string? Author { get; set; }
}
