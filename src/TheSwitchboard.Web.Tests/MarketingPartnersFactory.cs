using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TheSwitchboard.Web.Tests;

public class MarketingPartnersFactory : SwitchboardWebApplicationFactory
{
    private readonly string _dbName = "MarketingPartners-" + Guid.NewGuid();

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

    /// <summary>
    /// Produces an HttpClient with a valid admin session cookie — obtained by
    /// fetching the login form, extracting the antiforgery token, and posting
    /// credentials. Mirrors Slice3Factory.LoggedInClientAsync.
    /// </summary>
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
            throw new InvalidOperationException("MarketingPartnersFactory: admin login did not redirect");

        var authCookies = res.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]);
        var authed = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        authed.DefaultRequestHeaders.Add("Cookie", string.Join("; ", authCookies));
        return authed;
    }

    /// <summary>
    /// POSTs a form with a fresh antiforgery token scoped to the target page.
    /// Handles the dance of GET-then-POST that Razor Pages + AntiForgery require.
    /// </summary>
    public async Task<HttpResponseMessage> PostAsync(HttpClient client, string url, Dictionary<string, string> fields)
    {
        // GET the target URL's base page to get the AF token cookie + form field.
        var basePath = url.Split('?')[0];
        var getRes = await client.GetAsync(basePath);
        var getHtml = await getRes.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(getHtml, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        if (tokenMatch.Success)
        {
            fields = new Dictionary<string, string>(fields)
            {
                ["__RequestVerificationToken"] = tokenMatch.Groups[1].Value
            };
        }
        var content = new FormUrlEncodedContent(fields!);
        return await client.PostAsync(url, content);
    }
}
