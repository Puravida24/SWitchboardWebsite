using System.Collections.Concurrent;

namespace TheSwitchboard.Web.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    // Simple in-memory rate limiter. In production with Redis, swap for distributed.
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _clients = new();
    private const int MaxRequests = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>Test-only hook: clears the in-memory client bucket so each test starts fresh.</summary>
    public static void ResetAll() => _clients.Clear();

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only rate limit API/form endpoints
        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{clientIp}:{path}";
        var now = DateTime.UtcNow;

        var entry = _clients.GetOrAdd(key, _ => new RateLimitEntry(now, 0));

        // Reset window if expired
        if (now - entry.WindowStart > Window)
        {
            entry = new RateLimitEntry(now, 0);
            _clients[key] = entry;
        }

        if (entry.RequestCount >= MaxRequests)
        {
            _logger.LogWarning("Rate limit exceeded for {ClientIp} on {Path}", clientIp, path);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsync("Too many requests. Please try again later.");
            return;
        }

        _clients[key] = entry with { RequestCount = entry.RequestCount + 1 };
        await _next(context);
    }

    private record RateLimitEntry(DateTime WindowStart, int RequestCount);
}
