namespace TheSwitchboard.Web.Services.Tracking;

/// <summary>Runs <see cref="IInsightsService.DetectAsync"/> every 15 minutes.</summary>
public class InsightsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InsightsBackgroundService> _logger;
    public InsightsBackgroundService(IServiceProvider services, ILogger<InsightsBackgroundService> logger)
    { _services = services; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IInsightsService>();
                await svc.DetectAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogDebug(ex, "Insights tick failed"); }
            try { await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}

/// <summary>Runs <see cref="IAlertEvaluatorService.EvaluateAsync"/> every 5 minutes.</summary>
public class AlertEvaluatorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AlertEvaluatorBackgroundService> _logger;
    public AlertEvaluatorBackgroundService(IServiceProvider services, ILogger<AlertEvaluatorBackgroundService> logger)
    { _services = services; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IAlertEvaluatorService>();
                await svc.EvaluateAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogDebug(ex, "Alert eval tick failed"); }
            try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
