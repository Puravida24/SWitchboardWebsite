using System.Collections.Concurrent;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Tracks the rolling live-visitor counter for the Real-Time dashboard. A visitor
/// is considered "active" if we've seen a tracker event from their <c>sw_vid</c>
/// within the TTL window (default 120 seconds). Backed by an in-memory dictionary —
/// scales fine at current traffic; future: Redis set with EXPIRE for multi-instance.
/// </summary>
public interface IRealtimeMetrics
{
    void TouchVisitor(string visitorId, DateTime? nowUtc = null);
    int ActiveVisitorCount(DateTime? nowUtc = null);
    bool IsActive(string visitorId, DateTime? nowUtc = null);
}

public class RealtimeMetrics : IRealtimeMetrics
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(120);
    private readonly ConcurrentDictionary<string, DateTime> _lastSeen = new();

    public void TouchVisitor(string visitorId, DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(visitorId)) return;
        _lastSeen[visitorId] = nowUtc ?? DateTime.UtcNow;
    }

    public int ActiveVisitorCount(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var cutoff = now - Ttl;
        var count = 0;
        foreach (var kvp in _lastSeen)
        {
            if (kvp.Value >= cutoff) count++;
            else _lastSeen.TryRemove(kvp.Key, out _); // opportunistic cleanup
        }
        return count;
    }

    public bool IsActive(string visitorId, DateTime? nowUtc = null)
    {
        if (!_lastSeen.TryGetValue(visitorId, out var ts)) return false;
        var now = nowUtc ?? DateTime.UtcNow;
        return (now - ts) < Ttl;
    }
}
