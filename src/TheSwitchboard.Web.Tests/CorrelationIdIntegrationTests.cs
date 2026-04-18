using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// H-7.B / H-7.C — live middleware tests for the correlation-ID header.
/// </summary>
public class CorrelationIdIntegrationTests : IClassFixture<CorrelationIdIntegrationTests.LocalFactory>
{
    private readonly LocalFactory _factory;
    public CorrelationIdIntegrationTests(LocalFactory factory) => _factory = factory;

    public class LocalFactory : SwitchboardWebApplicationFactory
    {
        private readonly string _dbName = "CorrIdFactory-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:InMemoryName"] = _dbName
                }));
        }
    }

    [Fact]
    public async Task H7_B_Request_Without_Header_GetsOneGenerated()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        Assert.True(res.Headers.Contains("X-Correlation-ID"),
            "Response must include X-Correlation-ID even if request didn't send one.");
        var id = res.Headers.GetValues("X-Correlation-ID").First();
        Assert.False(string.IsNullOrWhiteSpace(id));
        // Looks like a GUID or compact base64 — at least 8 chars.
        Assert.True(id.Length >= 8);
    }

    [Fact]
    public async Task H7_C_Request_With_Header_EchoesSameValue()
    {
        var client = _factory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/health");
        const string sent = "test-correlation-value-xyz";
        req.Headers.Add("X-Correlation-ID", sent);
        var res = await client.SendAsync(req);
        Assert.True(res.Headers.Contains("X-Correlation-ID"));
        Assert.Equal(sent, res.Headers.GetValues("X-Correlation-ID").First());
    }
}
