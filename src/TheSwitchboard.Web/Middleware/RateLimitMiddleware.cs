namespace TheSwitchboard.Web.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    // Non-tracker API endpoints (contact, ses, phoenix, etc.) — tight budget.
    private const int MaxGeneralApi = 10;
    // Tracker ingest endpoints — per-sid bucket, higher budget because pageview /
    // click / signal volume is large but legit. Above this the client is probably
    // looping or a hostile tab is spamming.
    private const int MaxTrackerPerSid = 300;

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

        var isTracker = path.StartsWith("/api/tracking/", StringComparison.OrdinalIgnoreCase);
        string key;
        int cap;

        if (isTracker)
        {
            // Key on sw_sid when present so a shared IP doesn't get throttled
            // by someone else on the same network. Fallback to IP.
            var sid = context.Request.Cookies["sw_sid"];
            var bucket = !string.IsNullOrWhiteSpace(sid) ? sid : (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            key = $"trk:{bucket}";
            cap = MaxTrackerPerSid;
        }
        else
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            key = $"{clientIp}:{path}";
            cap = MaxGeneralApi;
        }

        var count = await store.IncrementAsync(key);
        if (count > cap)
        {
            if (isTracker)
                _logger.LogDebug("Tracker rate limit hit bucket={Bucket} path={Path}", key, path);
            else
                _logger.LogWarning("Rate limit exceeded for {Key} on {Path}", key, path);
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
