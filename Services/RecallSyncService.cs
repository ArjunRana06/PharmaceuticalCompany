using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using System.Globalization;
using System.Text.Json;

namespace Project.Services;

public class RecallSyncService : IRecallSyncService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly DdaRecallScraper _ddaScraper;
    private readonly ILogger<RecallSyncService> _logger;

    public RecallSyncService(
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context,
        IConfiguration configuration,
        DdaRecallScraper ddaScraper,
        ILogger<RecallSyncService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _configuration = configuration;
        _ddaScraper = ddaScraper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RecallFeedItem>> FetchLatestAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<RecallFeedItem>();
        var successes = 0;
        var failures = 0;

        var openFdaUrl = _configuration["RecallFeed:OpenFdaUrl"]
            ?? _configuration["RecallFeed:Url"];

        if (!string.IsNullOrWhiteSpace(openFdaUrl))
        {
            try
            {
                items.AddRange(await FetchOpenFdaAsync(openFdaUrl, cancellationToken));
                successes++;
            }
            catch (Exception ex)
            {
                failures++;
                _logger.LogError(ex, "Failed to fetch openFDA recall feed.");
            }
        }

        var ddaEnabled = _configuration.GetValue("RecallFeed:DdaEnabled", true);
        var ddaUrl = _configuration["RecallFeed:DdaCategoryUrl"];

        if (ddaEnabled && !string.IsNullOrWhiteSpace(ddaUrl))
        {
            try
            {
                var maxItems = _configuration.GetValue("RecallFeed:MaxItemsPerSource", 100);
                items.AddRange(await _ddaScraper.FetchLatestAsync(ddaUrl!, maxItems, cancellationToken));
                successes++;
            }
            catch (Exception ex)
            {
                failures++;
                _logger.LogError(ex, "Failed to fetch DDA recall feed.");
            }
        }

        if (successes == 0 && failures > 0)
        {
            throw new InvalidOperationException("All configured recall sources failed.");
        }

        return items
            .GroupBy(i => !string.IsNullOrWhiteSpace(i.ExternalId)
                ? $"{i.SourceName}|{i.ExternalId}"
                : $"{i.SourceName}|{i.SourceUrl}|{i.DrugName}|{i.RecallDate:yyyyMMdd}")
            .Select(g => g.First())
            .OrderByDescending(i => i.RecallDate)
            .ToList();
    }

    public async Task<int> SyncLatestAsync(CancellationToken cancellationToken = default)
    {
        var log = new RecallSyncLog
        {
            StartedAt = DateTime.Now,
            Status = "Running"
        };

        _context.RecallSyncLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        var saved = 0;
        var failed = 0;

        try
        {
            var items = await FetchLatestAsync(cancellationToken);
            log.ItemsFetched = items.Count;

            if (items.Count > 0)
            {
                var result = await SaveItemsAsync(items, cancellationToken);
                saved = result.Saved;
                failed = result.Failed;
            }

            log.ItemsSaved = saved;
            log.Status = failed == 0 ? "Success" : saved == 0 ? "Failed" : "Partial";
            log.CompletedAt = DateTime.Now;
            log.Message = $"Fetched {items.Count} item(s), saved {saved} item(s), failed {failed} item(s).";
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.CompletedAt = DateTime.Now;
            log.Message = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            _logger.LogError(ex, "Recall sync failed.");
            throw;
        }
        finally
        {
            await _context.SaveChangesAsync(CancellationToken.None);
        }

        return saved;
    }

    public async Task<int> SaveFetchedAsync(
        IReadOnlyList<RecallFeedItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return 0;
        }

        var result = await SaveItemsAsync(items, cancellationToken);
        return result.Saved;
    }

    private sealed record SaveResult(int Saved, int Failed);

    private async Task<SaveResult> SaveItemsAsync(
        IReadOnlyList<RecallFeedItem> items,
        CancellationToken cancellationToken)
    {
        var conn = _context.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await conn.OpenAsync(cancellationToken);
        }

        var count = 0;
        var failed = 0;
        try
        {
            foreach (var item in items)
            {
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "dbo.usp_SyncLatestRecalls";
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@DrugName", item.DrugName));
                    cmd.Parameters.Add(new SqlParameter("@BatchNumber", (object?)item.BatchNumber ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@Reason", (object?)item.Reason ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@RecallDate", item.RecallDate));
                    cmd.Parameters.Add(new SqlParameter("@RecallLevel", (object?)item.RecallLevel ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@SourceUrl", (object?)item.SourceUrl ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@SourceName", (object?)item.SourceName ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@ExternalId", (object?)item.ExternalId ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@CompanyName", (object?)item.CompanyName ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@SideEffects", (object?)item.SideEffects ?? DBNull.Value));

                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                    count++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "Failed to save recall item {DrugName} ({ExternalId}).", item.DrugName, item.ExternalId);
                }
            }
        }
        finally
        {
            if (shouldClose)
            {
                await conn.CloseAsync();
            }
        }

        return new SaveResult(count, failed);
    }

    private async Task<IReadOnlyList<RecallFeedItem>> FetchOpenFdaAsync(
        string url,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["RecallFeed:OpenFdaApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            url += (url.Contains('?') ? "&" : "?") + "api_key=" + Uri.EscapeDataString(apiKey);
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PharmaCore-RecallSync/1.0");

        string? json = null;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                json = await response.Content.ReadAsStringAsync(cancellationToken);
                break;
            }
            catch (Exception ex) when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "openFDA fetch attempt {Attempt} failed. Retrying.", attempt);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<RecallFeedItem>();
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<RecallFeedItem>();
        }

        var items = new List<RecallFeedItem>();

        foreach (var r in results.EnumerateArray())
        {
            var drugName = GetString(r, "product_description");
            if (string.IsNullOrWhiteSpace(drugName))
            {
                drugName = GetString(r, "openfda", "brand_name");
            }
            if (string.IsNullOrWhiteSpace(drugName))
            {
                drugName = "Unknown product";
            }

            var recallNumber = GetString(r, "recall_number");
            var reason = GetString(r, "reason_for_recall");
            var classification = GetString(r, "classification");
            var codeInfo = GetString(r, "code_info");
            var companyName = GetString(r, "recalling_firm");

            var recallDate = ParseOpenFdaDate(GetString(r, "recall_initiation_date"))
                ?? ParseOpenFdaDate(GetString(r, "report_date"))
                ?? DateTime.UtcNow.Date;

            var sourceUrl = string.IsNullOrWhiteSpace(recallNumber)
                ? url
                : $"https://api.fda.gov/drug/enforcement.json?search=recall_number:%22{Uri.EscapeDataString(recallNumber)}%22";

            items.Add(new RecallFeedItem
            {
                DrugName = Truncate(drugName, 200),
                BatchNumber = TruncateNullable(codeInfo, 100),
                Reason = reason,
                RecallDate = recallDate,
                RecallLevel = classification,
                SourceUrl = Truncate(sourceUrl, 500),
                SourceName = "US FDA",
                ExternalId = recallNumber,
                CompanyName = TruncateNullable(companyName, 200),
                SideEffects = null
            });
        }

        _logger.LogInformation("openFDA feed returned {Count} item(s).", items.Count);
        return items;
    }

    private static string? GetString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object ||
                !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        if (current.ValueKind == JsonValueKind.Array && current.GetArrayLength() > 0)
        {
            current = current[0];
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static DateTime? ParseOpenFdaDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParseExact(
            value,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value.Substring(0, maxLength);

    private static string? TruncateNullable(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) ? value : Truncate(value, maxLength);
}
