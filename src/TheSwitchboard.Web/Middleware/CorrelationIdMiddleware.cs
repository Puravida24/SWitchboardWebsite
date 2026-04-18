using Serilog.Context;

namespace TheSwitchboard.Web.Middleware;

/// <summary>
/// H-7.B/C: read or generate an X-Correlation-ID per request, attach to the
/// Serilog log context so every subsequent log line carries it, and echo it
/// on the response so clients can grep their own Seq query by ID.
///
/// Propagation rules:
///   - Honor inbound X-Correlation-ID if present (for distributed traces).
///   - Otherwise generate a compact 22-char base64 Guid.
///   - Always set the header on the response BEFORE the response starts,
///     via Response.OnStarting so late-started responses still carry it.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var inbound = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(inbound)
            ? Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                     .TrimEnd('=').Replace('+', '-').Replace('/', '_')
            : inbound!;

        context.Items["CorrelationId"] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
