using System.Text;
using System.Text.Json;

namespace TheSwitchboard.Web.Services;

/// <summary>
/// Submits URL changes to IndexNow — a push-based indexing protocol supported
/// by Bing, Yandex, Seznam, Naver (not Google). Shrinks the time-to-index from
/// days to minutes on supporting engines.
///
/// Requires <c>IndexNow:Key</c> config — a 32-128 char hex string. Verification
/// works by serving the key at <c>GET /{key}.txt</c> so the search engine can
/// confirm you own the domain.
/// </summary>
public interface IIndexNowService
{
    /// <summary>True if IndexNow:Key is configured.</summary>
    bool Enabled { get; }
    /// <summary>Returns the configured key if it matches, null otherwise.</summary>
    string? GetKeyIfMatches(string requested);
    /// <summary>POSTs a single URL update to api.indexnow.org. Fire-and-forget.</summary>
    Task SubmitUrlAsync(string url);
    /// <summary>Batch version — sends up to 10,000 URLs in one POST per spec.</summary>
    Task SubmitUrlsAsync(IEnumerable<string> urls);
}

public class IndexNowService : IIndexNowService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<IndexNowService> _logger;

    public IndexNowService(IConfiguration config, IHttpClientFactory http, ILogger<IndexNowService> logger)
    {
        _config = config;
        _http = http;
        _logger = logger;
    }

    public bool Enabled => !string.IsNullOrWhiteSpace(_config["IndexNow:Key"]);

    public string? GetKeyIfMatches(string requested)
    {
        var key = _config["IndexNow:Key"];
        if (string.IsNullOrWhiteSpace(key)) return null;
        // Exact match on the filename base (without .txt).
        return string.Equals(requested, key, StringComparison.Ordinal) ? key : null;
    }

    public Task SubmitUrlAsync(string url) => SubmitUrlsAsync(new[] { url });

    public async Task SubmitUrlsAsync(IEnumerable<string> urls)
    {
        var key = _config["IndexNow:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogDebug("IndexNow submit skipped — key not configured");
            return;
        }

        var host = _config["IndexNow:Host"] ?? "www.theswitchboardmarketing.com";
        var urlList = urls.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Take(10000).ToArray();
        if (urlList.Length == 0) return;

        var payload = new
        {
            host = host,
            key = key,
            keyLocation = $"https://{host}/{key}.txt",
            urlList = urlList
        };
        var body = JsonSerializer.Serialize(payload);

        try
        {
            using var client = _http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var res = await client.PostAsync("https://api.indexnow.org/indexnow", content);
            _logger.LogInformation("IndexNow submit {Count} urls → {Status}", urlList.Length, (int)res.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IndexNow submit failed");
        }
    }
}
