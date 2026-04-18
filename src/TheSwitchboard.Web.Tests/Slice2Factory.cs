using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheSwitchboard.Web.Middleware;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice 2 factory — extends the Slice 1 factory and replaces IPhoenixCrmService +
/// IEmailService with controllable fakes so Slice 2 behavior (webhook payload, retry,
/// email dispatch) can be asserted in-process without real SES / real CRM.
/// </summary>
public class Slice2Factory : SwitchboardWebApplicationFactory
{
    public FakePhoenixCrmService FakePhoenix { get; } = new();
    public FakeEmailService FakeEmail { get; } = new();

    public void ResetFakes()
    {
        FakePhoenix.Reset();
        FakeEmail.Reset();
        RateLimitMiddleware.ResetAll();
    }

    // Unique per-fixture DB name so Slice 2 tests don't see Slice 1's seeded rows
    // (and vice versa). InMemory shares DBs across hosts by name.
    private readonly string _dbName = "Slice2-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:InMemoryName"] = _dbName
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace registered services with our fakes.
            for (int i = services.Count - 1; i >= 0; i--)
            {
                var desc = services[i];
                if (desc.ServiceType == typeof(IPhoenixCrmService) ||
                    desc.ServiceType == typeof(IEmailService))
                {
                    services.RemoveAt(i);
                }
            }
            services.AddSingleton<IPhoenixCrmService>(FakePhoenix);
            services.AddSingleton<IEmailService>(FakeEmail);
        });
    }

    public async Task<HttpClient> LoggedInClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Admin/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = Regex.Match(html,
            @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        var cookieHeader = string.Join("; ",
            loginPage.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]));

        using var req = new HttpRequestMessage(HttpMethod.Post, "/Admin/Login");
        req.Headers.Add("Cookie", cookieHeader);
        req.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", "admin@theswitchboard.local"),
            new KeyValuePair<string, string>("Password", "SwitchboardDev2026!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });
        var res = await client.SendAsync(req);
        if (res.StatusCode != HttpStatusCode.Redirect)
            throw new InvalidOperationException("Slice2Factory: admin login did not redirect");

        var authCookies = res.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]);
        var authed = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        authed.DefaultRequestHeaders.Add("Cookie", string.Join("; ", authCookies));
        return authed;
    }
}
