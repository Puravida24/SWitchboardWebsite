using Microsoft.AspNetCore.SignalR;
using TheSwitchboard.Web.Hubs;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Fire-and-forget activity broadcaster. Pushed from SessionService.UpsertAsync
/// onto the SignalR <c>admin</c> group. Errors are swallowed — tracking must
/// never break the site.
/// </summary>
public interface IRealtimeBroadcaster
{
    Task BroadcastActivityAsync(ActivityEvent evt);
}

public sealed record ActivityEvent(
    string Kind,
    string? Path,
    string? VisitorId,
    string? SessionId,
    string? DeviceType,
    string? Browser,
    string? UtmSource,
    bool IsBot,
    DateTime Ts);

public class RealtimeBroadcaster : IRealtimeBroadcaster
{
    private readonly IHubContext<RealtimeHub> _hub;
    private readonly ILogger<RealtimeBroadcaster> _logger;

    public RealtimeBroadcaster(IHubContext<RealtimeHub> hub, ILogger<RealtimeBroadcaster> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task BroadcastActivityAsync(ActivityEvent evt)
    {
        try
        {
            await _hub.Clients.Group("admin").SendAsync("activity", evt);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Realtime broadcast failed for {Kind}", evt.Kind);
        }
    }
}
