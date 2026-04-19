using System.ComponentModel.DataAnnotations;

namespace TheSwitchboard.Web.Models.Tracking;

/// <summary>
/// rrweb session replay envelope. One row per replayed session (20% sample in
/// prod). Chunks live on <see cref="ReplayChunk"/> with a FK back to this row
/// so the admin player can stream them in sequence.
/// </summary>
public class Replay
{
    public long Id { get; set; }

    [Required, MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime EndedAt { get; set; } = DateTime.UtcNow;

    public int DurationSeconds { get; set; }
    public int ChunkCount { get; set; }
    public long ByteSize { get; set; }

    /// <summary>True when chunks are gzipped (default — client uses CompressionStream).</summary>
    public bool Compressed { get; set; } = true;
}
