using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Nightly pre-aggregated rollup row. Lets admin charts query daily buckets
/// instead of scanning raw events. Primary key is the composite of Date + Path +
/// Metric + Dimension — "pageviews for / on 2026-04-15", "clicks for /pricing",
/// "sessions:utm=linkedin for all paths".
/// </summary>
public class EventRollupDaily
{
    public DateTime Date { get; set; }

    [MaxLength(500)]
    public string Path { get; set; } = string.Empty;

    /// <summary>pageviews | sessions | clicks | form-events | errors | vital-lcp-p75 | conversions</summary>
    [MaxLength(40)]
    public string Metric { get; set; } = string.Empty;

    /// <summary>Optional secondary bucket — utm source, device type, etc. Empty when not applicable.</summary>
    [MaxLength(120)]
    public string Dimension { get; set; } = string.Empty;

    public long Value { get; set; }
}
