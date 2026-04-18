namespace TheSwitchboard.Web.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    private const int MaxRequests = 10;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitStore store)
    {
        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{clientIp}:{path}";
        var count = await store.IncrementAsync(key);
        if (count > MaxRequests)
        {
            _logger.LogWarning("Rate limit exceeded for {ClientIp} on {Path}", clientIp, path);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsync("Too many requests. Please try again later.");
            return;
        }
        await _next(context);
    }

    /// <summary>Test-only — clears the default in-memory store.</summary>
    public static void ResetAll() => InMemoryRateLimitStore.ResetAll();
}
