using System.Collections.Concurrent;

namespace TheSwitchboard.Web.Middleware;

/// <summary>
/// Single-process sliding-window rate limit. Window is 60s. State resets on app
/// restart and does not share across replicas — use <see cref="IRateLimitStore"/>'s
/// Redis implementation for multi-replica deployments.
/// </summary>
public class InMemoryRateLimitStore : IRateLimitStore
{
    private static readonly ConcurrentDictionary<string, Entry> _buckets = new();
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private record Entry(DateTime WindowStart, int Count);

    public Task<int> IncrementAsync(string key)
    {
        var now = DateTime.UtcNow;
        var result = _buckets.AddOrUpdate(
            key,
            _ => new Entry(now, 1),
            (_, e) => (now - e.WindowStart) > Window
                ? new Entry(now, 1)
                : e with { Count = e.Count + 1 });
        return Task.FromResult(result.Count);
    }

    /// <summary>Test hook — clears the bucket so tests start fresh.</summary>
    public static void ResetAll() => _buckets.Clear();
}
