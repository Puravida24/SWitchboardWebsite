using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Boots the real TheSwitchboard.Web app under Kestrel (in a child `dotnet` process)
/// on a free loopback port, then drives it with headless Chromium via Playwright.
///
/// Why a subprocess and not WebApplicationFactory? WAF internally casts its IServer
/// to Microsoft.AspNetCore.TestHost.TestServer; swapping that to Kestrel to get a
/// real socket for Playwright throws InvalidCastException on first HTTP access. A
/// subprocess keeps Program.cs untouched and runs production startup 1:1.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private Process? _serverProcess;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public string BaseUrl { get; private set; } = string.Empty;

    // Shared admin credentials for any Playwright test that needs to log in.
    // These flow into the child process as ASP.NET config via the `Admin__*`
    // env vars (double-underscore = section separator), so AdminSeedService
    // creates the user at boot. Same shape as SwitchboardWebApplicationFactory
    // uses for the non-browser integration tests.
    public const string AdminEmail    = "admin@playwright.local";
    public const string AdminPassword = "PlaywrightAdmin2026!";

    public async Task InitializeAsync()
    {
        EnsureChromiumInstalled();

        var port = GetFreeLoopbackPort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var webProjectPath = FindWebProjectPath();

        var psi = new ProcessStartInfo("dotnet", $"run --project \"{webProjectPath}\" --no-launch-profile -- --urls {BaseUrl}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Testing";
        psi.EnvironmentVariables["DOTNET_ROLL_FORWARD"] = "LatestMajor";
        psi.EnvironmentVariables["Admin__Email"]    = AdminEmail;
        psi.EnvironmentVariables["Admin__Password"] = AdminPassword;
        psi.EnvironmentVariables.Remove("PORT");
        psi.EnvironmentVariables.Remove("DATABASE_URL");
        psi.EnvironmentVariables.Remove("DATABASE_PRIVATE_URL");

        _serverProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet subprocess for Playwright fixture.");

        _serverProcess.OutputDataReceived += (_, e) => { if (e.Data is not null) Console.WriteLine($"[web] {e.Data}"); };
        _serverProcess.ErrorDataReceived  += (_, e) => { if (e.Data is not null) Console.Error.WriteLine($"[web!] {e.Data}"); };
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        await WaitForServerReadyAsync(BaseUrl, TimeSpan.FromSeconds(90));

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task<IPage> NewPageAsync()
    {
        if (_browser is null) throw new InvalidOperationException("Fixture not initialized.");
        var context = await _browser.NewContextAsync();
        return await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        try { if (_browser is not null) await _browser.CloseAsync(); } catch { /* noop */ }
        _playwright?.Dispose();

        if (_serverProcess is { HasExited: false })
        {
            try { _serverProcess.Kill(entireProcessTree: true); } catch { /* noop */ }
            try { await _serverProcess.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token); } catch { /* noop */ }
        }
        _serverProcess?.Dispose();
    }

    private static void EnsureChromiumInstalled()
    {
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        if (exitCode != 0)
            throw new InvalidOperationException($"Playwright `install chromium` exited {exitCode}.");
    }

    private static int GetFreeLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try { return ((IPEndPoint)listener.LocalEndpoint).Port; }
        finally { listener.Stop(); }
    }

    private static string FindWebProjectPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "TheSwitchboard.Web", "TheSwitchboard.Web.csproj");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate TheSwitchboard.Web.csproj by walking parents from AppContext.BaseDirectory.");
    }

    private static async Task WaitForServerReadyAsync(string baseUrl, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow + timeout;
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var res = await client.GetAsync(baseUrl + "/health");
                if ((int)res.StatusCode < 500) return;
            }
            catch (Exception ex) { last = ex; }
            await Task.Delay(500);
        }
        throw new TimeoutException($"Web server at {baseUrl} did not become ready within {timeout.TotalSeconds:F0}s. Last error: {last?.Message}");
    }
}
