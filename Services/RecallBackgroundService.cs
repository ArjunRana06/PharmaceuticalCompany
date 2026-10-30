using Microsoft.EntityFrameworkCore;
using Project.Data;

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
        if (!_configuration.GetValue("RecallFeed:Enabled", true))
        {
            _logger.LogInformation("Recall feed sync is disabled by configuration.");
            return;
        }

        var runAt = ParseRunTime(_configuration["RecallFeed:RunAtLocalTime"]);

        try
        {
            var lastSuccess = await GetLastSuccessfulSyncAsync(stoppingToken);
            if (lastSuccess == null || DateTime.Now - lastSuccess.Value >= TimeSpan.FromHours(24))
            {
                await RunSyncAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initial recall sync failed.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = GetNextRunTime(DateTime.Now, runAt);
                var delay = nextRun - DateTime.Now;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                await RunSyncAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled recall sync failed. Retrying in one hour.");
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sync = scope.ServiceProvider.GetRequiredService<IRecallSyncService>();
        var count = await sync.SyncLatestAsync(cancellationToken);
        _logger.LogInformation("Recall sync completed. Saved {Count} item(s).", count);
    }

    private async Task<DateTime?> GetLastSuccessfulSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.RecallSyncLogs
            .AsNoTracking()
            .Where(l => l.Status == "Success" && l.CompletedAt != null)
            .OrderByDescending(l => l.CompletedAt)
            .Select(l => l.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static TimeOnly ParseRunTime(string? value)
    {
        return TimeOnly.TryParse(value, out var time)
            ? time
            : new TimeOnly(2, 0);
    }

    private static DateTime GetNextRunTime(DateTime now, TimeOnly runAt)
    {
        var next = now.Date.Add(runAt.ToTimeSpan());
        return next <= now ? next.AddDays(1) : next;
    }
}
