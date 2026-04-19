using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// One row per user click. Batched by the client (clickstream.js) and shipped to
/// <c>/api/tracking/clicks</c> every 5 seconds or on <c>visibilitychange=hidden</c>.
///
/// Rage clicks are detected server-side at ingest: the third click on the same
/// <see cref="Selector"/> within a 500 ms window flips <see cref="IsRage"/> on
/// all three. Dead clicks are flagged client-side — a click whose 1-second
/// <c>MutationObserver</c> window observed no DOM change and no navigation.
///
/// Cap is 500 rows per session — anything beyond is silently dropped. Raw clickstream
/// is purged after 90 days by the T-10 retention job.
/// </summary>
public class ClickEvent
{
    public long Id { get; set; }

    [Required, MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? VisitorId { get; set; }

    [Required, MaxLength(500)]
    public string Path { get; set; } = "/";

    public DateTime Ts { get; set; } = DateTime.UtcNow;

    public int X { get; set; }
    public int Y { get; set; }
    public int ViewportW { get; set; }
    public int ViewportH { get; set; }
    public int PageW { get; set; }
    public int PageH { get; set; }

    /// <summary>CSS selector path — up to 6 levels with nth-of-type fallback.</summary>
    [Required, MaxLength(500)]
    public string Selector { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? TagName { get; set; }

    /// <summary>First 64 chars of the clicked element's text content — trimmed.</summary>
    [MaxLength(64)]
    public string? ElementText { get; set; }

    [MaxLength(2000)]
    public string? ElementHref { get; set; }

    /// <summary>0 = left, 1 = middle, 2 = right.</summary>
    public int MouseButton { get; set; }

    public bool IsRage { get; set; }
    public bool IsDead { get; set; }
}
