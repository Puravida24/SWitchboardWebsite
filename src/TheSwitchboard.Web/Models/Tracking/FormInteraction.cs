using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Per-field form event — drives the funnel + abandonment reports. Event kinds:
///   focus | blur | input | paste | error | submit | abandon
///
/// The contact form on design-32e is single-page, so we treat each named field
/// as a funnel step. Dwell/char/correction counters help spot "got stuck on
/// message" patterns without PII (the actual typed value is never persisted).
/// </summary>
public class FormInteraction
{
    public long Id { get; set; }

    [Required, MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? VisitorId { get; set; }

    [Required, MaxLength(500)]
    public string Path { get; set; } = "/";

    /// <summary>Form identifier from [data-tb-form-id] — e.g. "contact".</summary>
    [Required, MaxLength(64)]
    public string FormId { get; set; } = string.Empty;

    /// <summary>Field name from [data-tb-field] — e.g. "email", "message".</summary>
    [Required, MaxLength(64)]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>focus | blur | input | paste | error | submit | abandon</summary>
    [Required, MaxLength(20)]
    public string Event { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public int? DwellMs { get; set; }
    public int? CharCount { get; set; }
    public int? CorrectionCount { get; set; }
    public bool? PastedFlag { get; set; }

    [MaxLength(50)]
    public string? ErrorCode { get; set; }

    [MaxLength(200)]
    public string? ErrorMessage { get; set; }
}
