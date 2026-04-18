using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Durable cross-session visitor identity. Pseudonymous — the <see cref="Id"/> is the
/// <c>sw_vid</c> cookie value assigned on first visit and persists for ~1 year.
/// Email is never stored here (only hashed on <c>ConsentCertificate</c> — see T-7B).
/// </summary>
public class Visitor
{
    /// <summary>Matches the <c>sw_vid</c> cookie value. 24-char crypto-random.</summary>
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>Incremented each time a new session begins for this visitor.</summary>
    public int SessionCount { get; set; }

    /// <summary>First time this visitor completed a contact form (or any tracked goal).</summary>
    public DateTime? ConvertedAt { get; set; }

    /// <summary>Stable hash of deterministic fingerprint fields. Nullable — populated as identity signals arrive.</summary>
    [MaxLength(64)]
    public string? VisitorHash { get; set; }
}
