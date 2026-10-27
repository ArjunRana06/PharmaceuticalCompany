using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using System.Text.Json;

namespace Project.Services;

public class RecallSyncService : IRecallSyncService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecallSyncService> _logger;

    public RecallSyncService(
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<RecallSyncService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RecallFeedItem>> FetchLatestAsync(CancellationToken cancellationToken = default)
    {
        var url = _configuration["RecallFeed:Url"];
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("RecallFeed:Url is not configured. Skipping fetch.");
            return Array.Empty<RecallFeedItem>();
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PharmaCore-RecallSync/1.0");

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return Array.Empty<RecallFeedItem>();

            var items = new List<RecallFeedItem>();
            foreach (var r in results.EnumerateArray())
            {
                string drugName = r.TryGetProperty("product_description", out var pd) && pd.ValueKind == JsonValueKind.String
                    ? pd.GetString() ?? "Unknown"
                    : (r.TryGetProperty("openfda", out var ofda) && ofda.TryGetProperty("brand_name", out var bn) && bn.ValueKind == JsonValueKind.Array && bn.GetArrayLength() > 0
                        ? bn[0].GetString() ?? "Unknown"
                        : "Unknown");

                string? reason = r.TryGetProperty("reason_for_recall", out var rf) && rf.ValueKind == JsonValueKind.String ? rf.GetString() : null;
                string? level = r.TryGetProperty("classification", out var cl) && cl.ValueKind == JsonValueKind.String ? cl.GetString() : null;
                string? recallNumber = r.TryGetProperty("recall_number", out var rn) && rn.ValueKind == JsonValueKind.String ? rn.GetString() : null;
                string? codeInfo = r.TryGetProperty("code_info", out var ci) && ci.ValueKind == JsonValueKind.String ? ci.GetString() : null;

                DateTime recallDate = DateTime.UtcNow;
                if (r.TryGetProperty("recall_initiation_date", out var rid) && rid.ValueKind == JsonValueKind.String)
                {
                    var s = rid.GetString();
                    if (DateTime.TryParseExact(s, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
                        recallDate = dt;
                }

                string sourceUrl = string.IsNullOrEmpty(recallNumber)
                    ? url
                    : $"https://api.fda.gov/drug/enforcement.json?search=recall_number:\"{recallNumber}\"";

                items.Add(new RecallFeedItem
                {
                    DrugName = drugName.Length > 200 ? drugName.Substring(0, 200) : drugName,
                    BatchNumber = string.IsNullOrEmpty(codeInfo) ? null : (codeInfo.Length > 100 ? codeInfo.Substring(0, 100) : codeInfo),
                    Reason = reason,
                    RecallDate = recallDate,
                    RecallLevel = level,
                    SourceUrl = sourceUrl.Length > 500 ? sourceUrl.Substring(0, 500) : sourceUrl,
                    SideEffects = null
                });
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch recall feed from {Url}", url);
            return Array.Empty<RecallFeedItem>();
        }
    }

    public async Task<int> SyncLatestAsync(CancellationToken cancellationToken = default)
    {
        var items = await FetchLatestAsync(cancellationToken);
        if (items.Count == 0) return 0;

        var conn = _context.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose) await conn.OpenAsync(cancellationToken);

        int count = 0;
        try
        {
            foreach (var item in items)
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
                cmd.Parameters.Add(new SqlParameter("@CompanyId", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@SideEffects", (object?)item.SideEffects ?? DBNull.Value));

                await cmd.ExecuteNonQueryAsync(cancellationToken);
                count++;
            }
        }
        finally
        {
            if (shouldClose) await conn.CloseAsync();
        }

        return count;
    }
}
