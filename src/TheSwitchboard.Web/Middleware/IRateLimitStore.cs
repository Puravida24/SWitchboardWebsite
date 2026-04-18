namespace TheSwitchboard.Web.Middleware;

/// <summary>
/// Abstraction over the rate-limit counter so it can swap between in-memory (default,
/// single-process) and Redis (multi-replica) at startup via config.
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    /// Atomically increments the counter for <paramref name="key"/> within a sliding
    /// 60-second window and returns the post-increment value.
    /// </summary>
    Task<int> IncrementAsync(string key);
}
