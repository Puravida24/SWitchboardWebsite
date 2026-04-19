using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Per-session browser signals captured once on first pageview. One row per sid —
/// the /api/tracking/signals endpoint is idempotent, later POSTs for the same
/// session overwrite rather than insert.
///
/// Used for: device-class reports, fingerprint-shift fraud heuristics, Meta WebView
/// detection (ad-click attribution quirks), rough hardware bucket for performance
/// cohort analysis.
/// </summary>
public class BrowserSignal
{
    public long Id { get; set; }

    [Required, MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]  public string? Timezone { get; set; }
    [MaxLength(20)]  public string? Language { get; set; }
    public int? ColorDepth { get; set; }
    public int? HardwareConcurrency { get; set; }
    public int? DeviceMemory { get; set; }
    public int? TouchPoints { get; set; }
    public int? ScreenW { get; set; }
    public int? ScreenH { get; set; }
    public double? PixelRatio { get; set; }
    public bool? Cookies { get; set; }
    public bool? LocalStorage { get; set; }
    public bool? SessionStorage { get; set; }
    public bool? IsMetaWebview { get; set; }
    public bool? IsTikTokWebview { get; set; }

    [MaxLength(128)] public string? CanvasFingerprint { get; set; }
    [MaxLength(128)] public string? WebGLVendor { get; set; }
    [MaxLength(128)] public string? WebGLRenderer { get; set; }

    [MaxLength(50)]  public string? Battery { get; set; }
    [MaxLength(50)]  public string? Connection { get; set; }
}
