using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// WebApplicationFactory override that points ContentRoot at the web project directory so
/// Razor views + wwwroot/wireframes/*.html are resolvable during integration tests. Also
/// strips any DATABASE_URL / connection strings so the app falls back to the InMemory
/// provider (no local PostgreSQL required for tests).
/// </summary>
public class SwitchboardWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Point content root at the web project so Razor + wwwroot resolve.
        var solutionDir = GetSolutionRoot();
        var webRoot = Path.Combine(solutionDir, "src", "TheSwitchboard.Web");
        builder.UseContentRoot(webRoot);
        // Keep envt "Testing" — isolates it from Development appsettings and avoids any
        // local DB connection strings. Our Program.cs treats anything non-Development as
        // Production-adjacent, but also degrades gracefully to InMemory when no DATABASE_URL
        // is present.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Blank out any connection string that might otherwise be pulled from
                // appsettings or env vars at test time.
                ["ConnectionStrings:DefaultConnection"] = string.Empty,
                ["DATABASE_URL"] = string.Empty,
                ["DATABASE_PRIVATE_URL"] = string.Empty,
                // Known admin seed creds used by Slice1IntegrationTests.
                ["Admin:Email"] = "admin@theswitchboard.local",
                ["Admin:Password"] = "SwitchboardDev2026!",
                ["Database:InMemoryName"] = "Slice1-" + Guid.NewGuid()
            });
        });
    }

    private static string GetSolutionRoot()
    {
        // Walk up from the test assembly location until we find TheSwitchboardWeb.sln
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TheSwitchboardWeb.sln")))
        {
            dir = dir.Parent;
        }
        if (dir == null)
            throw new InvalidOperationException("Could not locate TheSwitchboardWeb.sln above the test assembly.");
        return dir.FullName;
    }
}
