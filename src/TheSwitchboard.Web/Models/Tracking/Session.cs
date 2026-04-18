using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Per-session aggregate. One row per <c>sw_sid</c> cookie (30-min sliding). Every
/// tracking event (ping, pageview, click, form event, error, vital) upserts this
/// row so downstream reports don't need to join to raw event tables for basics.
///
/// Geo columns are intentionally absent — the plan opted out of geo lookups on this
/// marketing site. DeviceType / Browser / Os populate from the UA parser in T-2.
/// </summary>
public class Session
{
    /// <summary>Matches the <c>sw_sid</c> cookie value.</summary>
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? VisitorId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime EndedAt { get; set; } = DateTime.UtcNow;

    public int DurationSeconds { get; set; }
    public int EngagedTimeSeconds { get; set; }
    public int PageCount { get; set; }
    public int EventCount { get; set; }

    [MaxLength(500)]
    public string? LandingPath { get; set; }

    [MaxLength(500)]
    public string? ExitPath { get; set; }

    [MaxLength(500)]
    public string? Referrer { get; set; }

    // Attribution captured at landing (T-2 wires these from PageView.Landing row).
    [MaxLength(200)] public string? UtmSource { get; set; }
    [MaxLength(200)] public string? UtmMedium { get; set; }
    [MaxLength(200)] public string? UtmCampaign { get; set; }
    [MaxLength(200)] public string? UtmTerm { get; set; }
    [MaxLength(200)] public string? UtmContent { get; set; }
    [MaxLength(200)] public string? Gclid { get; set; }
    [MaxLength(200)] public string? Fbclid { get; set; }
    [MaxLength(200)] public string? Msclkid { get; set; }

    /// <summary>SHA256(ip + salt). Never the raw IP — that only lives on ConsentCertificate (T-7B).</summary>
    [MaxLength(64)]
    public string? IpHash { get; set; }

    [MaxLength(20)] public string? DeviceType { get; set; }
    [MaxLength(50)] public string? Browser { get; set; }
    [MaxLength(50)] public string? BrowserVersion { get; set; }
    [MaxLength(50)] public string? Os { get; set; }
    [MaxLength(50)] public string? OsVersion { get; set; }

    public int? ViewportW { get; set; }
    public int? ViewportH { get; set; }

    // T-3 bot classification lands these columns.
    public bool IsBot { get; set; }
    [MaxLength(50)] public string? BotReason { get; set; }

    /// <summary>"none" | "dnt" | "gpc" — what privacy signals the visitor sent.</summary>
    [MaxLength(20)]
    public string ConsentState { get; set; } = "none";

    public bool Converted { get; set; }
}
