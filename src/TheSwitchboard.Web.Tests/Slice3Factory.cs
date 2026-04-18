using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice 3 factory — unique per-fixture InMemory DB, no service overrides (Slice 3
/// exercises real CMS paths directly against the DB).
/// </summary>
public class Slice3Factory : SwitchboardWebApplicationFactory
{
    private readonly string _dbName = "Slice3-" + Guid.NewGuid();

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
    }

    public async Task<HttpClient> LoggedInClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Admin/Login");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        var cookieHeader = string.Join("; ",
            loginPage.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]));

        using var req = new HttpRequestMessage(HttpMethod.Post, "/Admin/Login");
        req.Headers.Add("Cookie", cookieHeader);
        req.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", "admin@theswitchboard.local"),
            new KeyValuePair<string, string>("Password", "SwitchboardDev2026!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        var res = await client.SendAsync(req);
        if (res.StatusCode != HttpStatusCode.Redirect)
            throw new InvalidOperationException("Slice3Factory: admin login did not redirect");

        var authCookies = res.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]);
        var authed = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        authed.DefaultRequestHeaders.Add("Cookie", string.Join("; ", authCookies));
        return authed;
    }
}
