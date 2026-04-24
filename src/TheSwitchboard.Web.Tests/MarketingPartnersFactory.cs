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
}
