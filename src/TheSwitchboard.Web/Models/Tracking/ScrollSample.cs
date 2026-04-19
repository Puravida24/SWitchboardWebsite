using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Scroll-depth milestone or max-depth-at-unload sample. One row per distinct
/// (SessionId, Path, Depth) — milestones fire at most once per page visit.
/// </summary>
public class ScrollSample
{
    public long Id { get; set; }

    [Required, MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Path { get; set; } = "/";

    public DateTime Ts { get; set; } = DateTime.UtcNow;

    /// <summary>Milestone bucket: 25 / 50 / 75 / 100 (or any fine-grained max depth).</summary>
    public int Depth { get; set; }

    /// <summary>Max depth reached on this page — only populated on unload sample.</summary>
    public int MaxDepth { get; set; }

    public int ViewportH { get; set; }
    public int DocumentH { get; set; }
    public int TimeSinceLoadMs { get; set; }
}
