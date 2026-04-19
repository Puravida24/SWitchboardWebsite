using Microsoft.EntityFrameworkCore;
using TheSwitchboard.Web.Data;
using TheSwitchboard.Web.Models.Tracking;

namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>
/// Startup seeder — inserts the 3 canonical T-12 alert rules on an empty DB.
///   capture-rate-1h &lt; 95  → TCPA risk (phone submissions without certs)
///   bot-rate-1h &gt; 5        → scraper flood
///   js-errors-1h &gt; 20      → prod regression
/// Idempotent — each rule is keyed by its unique Name.
/// </summary>
public class DefaultAlertRulesSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DefaultAlertRulesSeeder> _logger;

    public DefaultAlertRulesSeeder(IServiceProvider services, ILogger<DefaultAlertRulesSeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var defaults = new[]
            {
                new AlertRule
                {
                    Name = "capture-rate-below-95",
                    MetricExpression = "capture-rate-1h",
                    Comparison = "lt",
                    Threshold = 95,
                    Window = "1h",
                    Enabled = true,
                    Channel = "email"
                },
                new AlertRule
                {
                    Name = "bot-rate-above-5",
                    MetricExpression = "bot-rate-1h",
                    Comparison = "gt",
                    Threshold = 5,
                    Window = "1h",
                    Enabled = true,
                    Channel = "email"
                },
                new AlertRule
                {
                    Name = "js-errors-above-20",
                    MetricExpression = "js-errors-1h",
                    Comparison = "gt",
                    Threshold = 20,
                    Window = "1h",
                    Enabled = true,
                    Channel = "email"
                }
            };

            foreach (var d in defaults)
            {
                if (!await db.AlertRules.AnyAsync(r => r.Name == d.Name, cancellationToken))
                {
                    db.AlertRules.Add(d);
                }
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Default alert rules seed skipped");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
