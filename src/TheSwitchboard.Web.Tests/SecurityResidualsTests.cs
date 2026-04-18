using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TheSwitchboard.Web.Middleware;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Tests;

/// <summary>
/// Slice H-3 — Security residuals. Fills three gaps the earlier hardening passes
/// (H-01 through H-07) missed:
///   A. /api/demo now rejects cross-origin POST (matches /api/contact).
///   B. /api/ses/bounce requires a valid HMAC-SHA256 signature.
///   C. ImageService.DeleteImage refuses path-traversal (../) attempts.
/// </summary>
public class SecurityResidualsTests
{
    private const string WebhookSecret = "test-webhook-secret-v1";

    private class H3Factory : SwitchboardWebApplicationFactory
    {
        private readonly string _dbName = "H3-" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:InMemoryName"] = _dbName,
                    ["Ses:WebhookSecret"] = WebhookSecret
                });
            });
        }
    }

    // ── H-3.A — /api/demo origin check ─────────────────────────────────────
    [Fact]
    public async Task H3_A_Demo_CrossOrigin_Post_Rejected_403()
    {
        RateLimitMiddleware.ResetAll();
        using var factory = new H3Factory();
        var evil = factory.CreateClient();
        evil.DefaultRequestHeaders.Add("Origin", "https://evil.example.com");
        var res = await evil.PostAsJsonAsync("/api/demo", new
        {
            firstName = "x",
            lastName = "y",
            email = "a@b.com",
            selectedDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
            selectedTime = "10:00"
        });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── H-3.B — /api/ses/bounce missing signature ─────────────────────────
    [Fact]
    public async Task H3_B_SesBounce_NoSignature_Rejected_401()
    {
        RateLimitMiddleware.ResetAll();
        using var factory = new H3Factory();
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/ses/bounce", new { email = "bounce@x.com" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── H-3.B — /api/ses/bounce wrong signature ──────────────────────────
    [Fact]
    public async Task H3_B_SesBounce_InvalidSignature_Rejected_401()
    {
        RateLimitMiddleware.ResetAll();
        using var factory = new H3Factory();
        var client = factory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/ses/bounce")
        {
            Content = JsonContent.Create(new { email = "bounce@x.com" })
        };
        req.Headers.Add("X-SES-Signature", "sha256=deadbeef");
        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── H-3.B — /api/ses/bounce valid signature → 200 ────────────────────
    [Fact]
    public async Task H3_B_SesBounce_ValidSignature_Accepted_200()
    {
        RateLimitMiddleware.ResetAll();
        using var factory = new H3Factory();
        var client = factory.CreateClient();

        var body = "{\"email\":\"bounce@example.com\"}";
        var sig = ComputeHmacSha256(body, WebhookSecret);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/ses/bounce")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("X-SES-Signature", $"sha256={sig}");
        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    private static string ComputeHmacSha256(string body, string secret)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = h.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ── H-3.C — ImageService.DeleteImage rejects path traversal ──────────
    [Fact]
    public void H3_C_ImageService_DeleteRejectsPathTraversal()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "switchboard-h3-" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);
        try
        {
            // Create a sentinel file OUTSIDE wwwroot that must NOT be deletable via
            // a crafted relative path.
            var sentinelDir = Path.GetDirectoryName(tempRoot)!;
            var sentinel = Path.Combine(sentinelDir, "sentinel-do-not-delete.txt");
            File.WriteAllText(sentinel, "precious");

            var env = new FakeEnv(tempRoot);
            var svc = new ImageService(env, NullLogger<ImageService>.Instance);

            // Craft a traversal path: from /tmp/switchboard-h3-XYZ, go up one level,
            // target the sentinel.
            var malicious = "../sentinel-do-not-delete.txt";

            // Must NOT throw (service should silently reject), AND sentinel must survive.
            svc.DeleteImage(malicious);

            Assert.True(File.Exists(sentinel),
                "ImageService.DeleteImage must refuse to delete files outside WebRootPath.");
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
            var sentinelDir2 = Path.GetDirectoryName(tempRoot)!;
            try { File.Delete(Path.Combine(sentinelDir2, "sentinel-do-not-delete.txt")); } catch { }
        }
    }

    private class FakeEnv : IWebHostEnvironment
    {
        public FakeEnv(string webRoot)
        {
            WebRootPath = webRoot;
            WebRootFileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot);
        }
        public string WebRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; }
        public string ApplicationName { get; set; } = "Test";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Testing";
    }
}
