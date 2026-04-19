using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// CCPA / GDPR delete request audit. One row per fulfilled request. DeletedRowCounts
/// is a JSON map of table-name → row-count so legal can show proof of erasure.
/// </summary>
public class DataSubjectRequest
{
    public long Id { get; set; }

    [Required, MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledAt { get; set; }

    /// <summary>pending | processing | complete | denied</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    /// <summary>JSON object mapping table-name to rows deleted.</summary>
    [MaxLength(2000)]
    public string DeletedRowCounts { get; set; } = "{}";
}
