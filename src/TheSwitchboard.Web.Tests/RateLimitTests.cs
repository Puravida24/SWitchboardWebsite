using Microsoft.AspNetCore.Http;
using TheSwitchboard.Web.Middleware;

namespace TheSwitchboard.Web.Tests;

public class RateLimitTests
{
    [Fact]
    public async Task NonApi_Request_Is_Not_Rate_Limited()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/about";
        var nextCalled = false;

        var middleware = new RateLimitMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<RateLimitMiddleware>());

        await middleware.InvokeAsync(context);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Api_Request_Passes_Under_Limit()
    {
        var context = CreateApiContext("/api/contact", "192.168.1.100");
        var nextCalled = false;

        var middleware = new RateLimitMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<RateLimitMiddleware>());

        await middleware.InvokeAsync(context);
        Assert.True(nextCalled);
        Assert.NotEqual(429, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateApiContext(string path, string ip)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        return context;
    }
}
