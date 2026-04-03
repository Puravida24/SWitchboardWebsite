using System.Text;
using System.Text.Json;

namespace TheSwitchboard.Web.Services;

public interface IPhoenixCrmService
{
    Task<bool> SendFormSubmissionAsync(string formType, Dictionary<string, string> data);
}

public class PhoenixCrmService : IPhoenixCrmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PhoenixCrmService> _logger;
    private readonly string? _webhookUrl;

    public PhoenixCrmService(HttpClient httpClient, IConfiguration configuration, ILogger<PhoenixCrmService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _webhookUrl = configuration["PhoenixCrm:WebhookUrl"];
    }

    public async Task<bool> SendFormSubmissionAsync(string formType, Dictionary<string, string> data)
    {
        if (string.IsNullOrEmpty(_webhookUrl))
        {
            _logger.LogWarning("Phoenix CRM webhook URL not configured. Skipping submission.");
            return false;
        }

        var payload = new
        {
            formType,
            data,
            source = "switchboard-website",
            submittedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(_webhookUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Form submission sent to Phoenix CRM: {FormType}", formType);
                return true;
            }

            _logger.LogWarning("Phoenix CRM returned {StatusCode} for {FormType}", response.StatusCode, formType);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send form submission to Phoenix CRM: {FormType}", formType);
            return false;
        }
    }
}
