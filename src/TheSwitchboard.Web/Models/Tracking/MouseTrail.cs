using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Sampled mouse position (200 ms interval from client). Capped at 300 rows per
/// session server-side — the nightly T-10 roll-up aggregates these into the heatmap.
/// </summary>
public class MouseTrail
{
    public long Id { get; set; }

    [Required, MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Path { get; set; } = "/";

    public DateTime Ts { get; set; } = DateTime.UtcNow;

    public int X { get; set; }
    public int Y { get; set; }
    public int ViewportW { get; set; }
    public int ViewportH { get; set; }
}
