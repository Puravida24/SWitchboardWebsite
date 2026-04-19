using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Analytics;

/// <summary>
/// One row per human pageview. Originally written by AnalyticsMiddleware for no-JS
/// visitors; T-2 adds a client-side /api/tracking/pageview path that captures UTM,
/// click IDs, viewport, and parsed UA so the admin surfaces can group by attribution.
/// </summary>
public class PageView
{
    public long Id { get; set; }
    public required string Path { get; set; }
    [MaxLength(2000)] public string? Referrer { get; set; }
    [MaxLength(500)]  public string? UserAgent { get; set; }
    [MaxLength(64)]   public string? IpHash { get; set; }
    [MaxLength(2)]    public string? Country { get; set; }
    [MaxLength(64)]   public string? SessionId { get; set; }
    public int? ScrollDepthPercent { get; set; }
    public int? TimeOnPageSeconds { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // T-2 attribution.
    [MaxLength(64)]   public string? VisitorId { get; set; }
    [MaxLength(200)]  public string? UtmSource { get; set; }
    [MaxLength(200)]  public string? UtmMedium { get; set; }
    [MaxLength(200)]  public string? UtmCampaign { get; set; }
    [MaxLength(200)]  public string? UtmTerm { get; set; }
    [MaxLength(200)]  public string? UtmContent { get; set; }
    [MaxLength(200)]  public string? Gclid { get; set; }
    [MaxLength(200)]  public string? Fbclid { get; set; }
    [MaxLength(200)]  public string? Msclkid { get; set; }

    /// <summary>First pageview in a session — used by landing-page + entry-path reports.</summary>
    public bool LandingFlag { get; set; }

    // T-2 parsed UA bucket.
    [MaxLength(20)] public string? DeviceType { get; set; }
    [MaxLength(50)] public string? Browser { get; set; }
    [MaxLength(50)] public string? Os { get; set; }

    public int? ViewportW { get; set; }
    public int? ViewportH { get; set; }
}
