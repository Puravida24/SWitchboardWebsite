using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Core Web Vital sample — LCP, FCP, CLS, INP, TTFB. Rated server-side against
/// Google's public thresholds (good / ni / poor) so admin pages don't need to
/// re-compute. One row per (session, metric, navigation) — a single-page app
/// that emits multiple INP values would get one row per sample.
/// </summary>
public class WebVitalSample
{
    public long Id { get; set; }

    [Required, MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Path { get; set; } = "/";

    public DateTime Ts { get; set; } = DateTime.UtcNow;

    /// <summary>LCP | FCP | CLS | INP | TTFB</summary>
    [Required, MaxLength(10)]
    public string Metric { get; set; } = string.Empty;

    public double Value { get; set; }

    /// <summary>good | ni | poor — bucket per Google Web Vitals thresholds.</summary>
    [Required, MaxLength(10)]
    public string Rating { get; set; } = "good";

    [MaxLength(20)] public string? NavigationType { get; set; }
    [MaxLength(64)] public string? NavId { get; set; }
}
