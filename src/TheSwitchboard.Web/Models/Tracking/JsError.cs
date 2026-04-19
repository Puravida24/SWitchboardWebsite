using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Uncaught JS error or unhandled rejection. Deduped server-side by
/// <see cref="Fingerprint"/> = sha256(message + source + line)[..16]. Duplicate
/// reports increment <see cref="Count"/> rather than inserting new rows.
/// </summary>
public class JsError
{
    public long Id { get; set; }

    [Required, MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Path { get; set; } = "/";

    public DateTime Ts { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? StackRedacted { get; set; }

    [MaxLength(500)] public string? Source { get; set; }
    public int? Line { get; set; }
    public int? Col { get; set; }

    [MaxLength(500)] public string? UserAgent { get; set; }
    [MaxLength(20)]  public string? BuildId { get; set; }

    /// <summary>16-char SHA256 prefix grouping duplicates.</summary>
    [MaxLength(16)]
    public string? Fingerprint { get; set; }

    /// <summary>How many times this fingerprint has fired in-window.</summary>
    public int Count { get; set; } = 1;
}
