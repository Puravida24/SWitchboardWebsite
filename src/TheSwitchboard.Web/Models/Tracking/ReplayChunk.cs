using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// rrweb event chunk — compressed (gzip) payload sized to stay under 512 KB
/// per request. Postgres TOAST compresses on top. Streamed sequentially by
/// the admin rrweb-player.
/// </summary>
public class ReplayChunk
{
    public long Id { get; set; }

    public long ReplayId { get; set; }

    [ForeignKey(nameof(ReplayId))]
    public Replay? Replay { get; set; }

    public int Sequence { get; set; }

    public DateTime Ts { get; set; } = DateTime.UtcNow;

    /// <summary>gzip-compressed byte payload. Persisted as BYTEA in Postgres.</summary>
    [Required]
    public byte[] Payload { get; set; } = Array.Empty<byte>();
}
