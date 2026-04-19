using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// TCPA-grade consent proof. One row per form submission that carries a phone
/// number + a "you consent to receive calls" disclosure. Captures the disclosure
/// snapshot at submit-time, environment, click coords, behavioral signals, and
/// SHA-256-hashed email/phone so Phoenix can later match on a dial.
///
/// Legal shelf-life: 5 years from <see cref="CreatedAt"/> — <see cref="ExpiresAt"/>
/// is exempt from the T-10 retention job's 1-year default.
/// </summary>
public class ConsentCertificate
{
    public long Id { get; set; }

    /// <summary>Shareable public identifier — "sw_cert_<24-char-random>".</summary>
    [Required, MaxLength(64)]
    public string CertificateId { get; set; } = string.Empty;

    /// <summary>Links back to the FormSubmission when available.</summary>
    public int? FormSubmissionId { get; set; }

    [MaxLength(64)] public string? SessionId { get; set; }

    public DateTime ConsentTimestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(40)] public string? ConsentMethod { get; set; }
    [MaxLength(500)] public string? ConsentElementSelector { get; set; }
    public int? ClickX { get; set; }
    public int? ClickY { get; set; }

    public DateTime? PageLoadedAt { get; set; }
    public int? TimeOnPageSeconds { get; set; }

    // Disclosure snapshot
    [Required, MaxLength(2000)]
    public string DisclosureText { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string DisclosureTextHash { get; set; } = string.Empty;

    public long? DisclosureVersionId { get; set; }

    [MaxLength(20)] public string? DisclosureFontSize { get; set; }
    [MaxLength(30)] public string? DisclosureColor { get; set; }
    [MaxLength(30)] public string? DisclosureBackgroundColor { get; set; }
    public double? DisclosureContrastRatio { get; set; }
    public bool DisclosureIsVisible { get; set; }

    // Env
    [MaxLength(64)]  public string? IpAddress { get; set; }
    [MaxLength(500)] public string? UserAgent { get; set; }
    [MaxLength(50)]  public string? BrowserName { get; set; }
    [MaxLength(50)]  public string? OsName { get; set; }
    [MaxLength(30)]  public string? ScreenResolution { get; set; }
    public int? ViewportW { get; set; }
    public int? ViewportH { get; set; }

    [MaxLength(2000)] public string? PageUrl { get; set; }

    // Behavioral signals
    public int? KeystrokesPerMinute { get; set; }
    public int? FormFieldsInteracted { get; set; }
    public int? MouseDistancePx { get; set; }
    public int? ScrollDepthPercent { get; set; }
    public bool IsSuspiciousBot { get; set; }

    // Hashed PII — SHA-256 hex, computed client-side.
    [MaxLength(64)] public string? EmailHash { get; set; }
    [MaxLength(64)] public string? PhoneHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddYears(5);
}
