namespace Project.Services;

public class RecallBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecallBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public RecallBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RecallBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabledStr = _configuration["RecallFeed:Enabled"];
        if (!bool.TryParse(enabledStr, out var enabled) || !enabled)
        {
            _logger.LogInformation("RecallFeed sync disabled by configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<IRecallSyncService>();
                var count = await sync.SyncLatestAsync(stoppingToken);
                _logger.LogInformation("Recall sync completed. Processed {Count} items.", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recall sync background job.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
