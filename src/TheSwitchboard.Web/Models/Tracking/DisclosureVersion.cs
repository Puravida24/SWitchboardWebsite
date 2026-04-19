using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Tracked version of the consent disclosure text. Auto-created on first
/// observation of a new text-hash; promoted to "registered" by legal review
/// in /Admin/Reports/Disclosures.
/// </summary>
public class DisclosureVersion
{
    public long Id { get; set; }

    /// <summary>Human version label — "v1", "v2", etc. Assigned on insert.</summary>
    [Required, MaxLength(20)]
    public string Version { get; set; } = "v1";

    [Required, MaxLength(64)]
    public string TextHash { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string FullText { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }

    [MaxLength(120)] public string? CreatedBy { get; set; }

    /// <summary>auto-detected | registered | retired</summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "auto-detected";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
