using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// Seed list of ASNs (Autonomous System Numbers) that host VPNs, proxies, Tor exits,
/// and cloud datacenters. Referenced by IP→ASN lookups in future slices for bot /
/// proxy classification.
///
/// T-3 creates the table and seeds a small starter list. Full enrichment + IP→ASN
/// resolver is deferred — current bot classification is UA-heuristic only, which
/// is sufficient while traffic is low.
/// </summary>
public class KnownProxyAsn
{
    [Key]
    public int Asn { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>"vpn" | "proxy" | "tor" | "datacenter" | "hosting".</summary>
    [Required, MaxLength(20)]
    public string Category { get; set; } = "hosting";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
