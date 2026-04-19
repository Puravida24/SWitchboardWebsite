using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TheSwitchboard.Web.Hubs;

/// <summary>
/// Admin-only SignalR hub. Every tracker event (pageview, click, form event, error)
/// fans out to the "admin" group so the /Admin/Reports/RealTime page can stream
/// them into its event tape live.
/// </summary>
[Authorize(Policy = "AdminPolicy")]
public class RealtimeHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin");
        await base.OnDisconnectedAsync(exception);
    }
}
