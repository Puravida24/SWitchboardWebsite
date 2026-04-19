using TheSwitchboard.Web.Services.Phoenix;

namespace TheSwitchboard.Web.Models.Forms;

public class FormSubmission
{
    public int Id { get; set; }
    public required string FormType { get; set; }
    public required string Data { get; set; }
    public string? SourcePage { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    /// <summary>Legacy — prefer <see cref="PhoenixSyncStatus"/>. Kept for back-compat with any prior writes.</summary>
    public bool SentToPhoenix { get; set; }
    public string? PhoenixResponse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Slice 2 additions ────────────────────────────────────────────
    /// <summary>Caller-provided role (carrier / agency / mga / other). Null for non-contact forms.</summary>
    public string? Role { get; set; }
    /// <summary>Email hit a hard bounce from SES → visible in admin, future emails suppressed.</summary>
    public bool BouncedEmail { get; set; }
    /// <summary>Dispatch state to Phoenix CRM webhook.</summary>
    public PhoenixSyncStatus PhoenixSyncStatus { get; set; } = PhoenixSyncStatus.Pending;
    /// <summary>Count of attempted webhook dispatches (includes the initial call). Max 3 before dead-letter.</summary>
    public int PhoenixSyncAttempts { get; set; }
    /// <summary>Timestamp of the most recent webhook attempt (used by the retry worker to decide when to retry).</summary>
    public DateTime? LastPhoenixAttemptAt { get; set; }

    // ── T-7B TCPA consent ───────────────────────────────────────────
    /// <summary>Links to the ConsentCertificate captured at submit time (nullable for pre-T7B submissions).</summary>
    public long? ConsentCertificateId { get; set; }
}
