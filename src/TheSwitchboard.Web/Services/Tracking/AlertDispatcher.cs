using TheSwitchboard.Web.Models.Tracking;
using TheSwitchboard.Web.Services;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Delivers AlertRule firings to an out-of-band channel (email or webhook).
/// Hub for future Slack / PagerDuty / SMS — swap in by channel kind.
/// </summary>
public interface IAlertDispatcher
{
    Task DispatchAsync(AlertRule rule, AlertLog log);
}

public class AlertDispatcher : IAlertDispatcher
{
    private readonly IEmailService _email;
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<AlertDispatcher> _logger;

    public AlertDispatcher(IEmailService email, IHttpClientFactory http, IConfiguration config, ILogger<AlertDispatcher> logger)
    {
        _email = email; _http = http; _config = config; _logger = logger;
    }

    public async Task DispatchAsync(AlertRule rule, AlertLog log)
    {
        var channel = (rule.Channel ?? "email").ToLowerInvariant();
        var target = rule.ChannelTarget;
        if (string.IsNullOrWhiteSpace(target))
        {
            target = channel == "email"
                ? _config["Email:InternalNotificationAddress"] ?? "jmarks@theswitchboardmarketing.com"
                : null;
        }
        if (string.IsNullOrWhiteSpace(target))
        {
            _logger.LogWarning("Alert {Rule} fired but no channel target configured", rule.Name);
            return;
        }

        try
        {
            if (channel == "email")
            {
                var subject = $"[{log.Severity.ToUpperInvariant()}] Switchboard alert: {rule.Name}";
                var body = $"""
                    <h2>Alert fired: {System.Net.WebUtility.HtmlEncode(rule.Name)}</h2>
                    <p><strong>Severity:</strong> {log.Severity}</p>
                    <p><strong>Metric:</strong> {rule.MetricExpression}</p>
                    <p><strong>Observed:</strong> {log.Value} {rule.Comparison} {log.Threshold}</p>
                    <p><strong>Window:</strong> {rule.Window}</p>
                    <p><strong>Fired at:</strong> {log.FiredAt:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p><a href="https://www.theswitchboardmarketing.com/Admin/Reports/Alerts">Open Alerts dashboard →</a></p>
                    """;
                await _email.SendAsync(target!, subject, body);
            }
            else if (channel == "webhook")
            {
                using var client = _http.CreateClient();
                var payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    rule = rule.Name,
                    severity = log.Severity,
                    metric = rule.MetricExpression,
                    value = log.Value,
                    threshold = log.Threshold,
                    firedAt = log.FiredAt,
                    context = log.Context
                });
                using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync(target, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Alert dispatch failed for rule {Rule}", rule.Name);
        }
    }
}
