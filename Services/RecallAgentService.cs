using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Project.Models;

namespace Project.Services;

public interface IRecallAgentService
{
    Task<RecallAgentResult> RunAsync(CancellationToken cancellationToken = default);
}

public class RecallAgentService : IRecallAgentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IRecallSyncService _syncService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecallAgentService> _logger;

    public RecallAgentService(
        IRecallSyncService syncService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RecallAgentService> logger)
    {
        _syncService = syncService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RecallAgentResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var provider = _configuration["RecallAgent:Provider"] ?? "Ollama";
        var model = _configuration["RecallAgent:Model"] ?? "qwen2.5-coder:3b";

        var result = new RecallAgentResult
        {
            Provider = provider,
            Model = model,
            RunAt = DateTime.Now
        };

        if (!_configuration.GetValue("RecallAgent:Enabled", true))
        {
            result.Message = "The AI recall agent is disabled in configuration.";
            return result;
        }

        IReadOnlyList<RecallFeedItem> items;
        try
        {
            items = await _syncService.FetchLatestAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI recall agent failed to fetch recall data.");
            result.Message = "The AI agent could not fetch recall data from the configured sources.";
            return result;
        }

        if (items.Count == 0)
        {
            result.Message = "No recall data was returned by the configured sources.";
            return result;
        }

        var sorted = items
            .OrderByDescending(i => i.RecallDate)
            .ThenBy(i => i.DrugName)
            .ToList();

        result.FetchedCount = sorted.Count;
        result.SourceCounts = sorted
            .GroupBy(i => string.IsNullOrWhiteSpace(i.SourceName) ? "Manual" : i.SourceName!)
            .ToDictionary(g => g.Key, g => g.Count());
        result.Items = sorted.Take(10).Select(ToAgentItem).ToList();

        try
        {
            result.SavedCount = await _syncService.SaveFetchedAsync(sorted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI recall agent failed to save fetched recall data.");
        }

        try
        {
            var aiOutput = await AnalyzeAsync(sorted, provider, model, cancellationToken);

            if (aiOutput == null)
            {
                ApplyFallback(result, sorted, "The AI model did not return a usable analysis.");
                return result;
            }

            result.AiUsed = true;
            result.Success = true;
            result.Summary = aiOutput.Summary.Trim();
            result.RiskLevel = NormalizeRiskLevel(aiOutput.RiskLevel);
            result.RecommendedAction = aiOutput.RecommendedAction.Trim();
            result.Highlights = MapHighlights(aiOutput.HighlightDrugNames, sorted);
            result.Message = $"AI agent fetched {result.FetchedCount} recall item(s) and saved {result.SavedCount}.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI recall analysis was unavailable.");
            ApplyFallback(result, sorted, $"AI analysis was unavailable ({ex.GetType().Name}).");
        }

        return result;
    }

    private async Task<RecallAgentAiOutput?> AnalyzeAsync(
        IReadOnlyList<RecallFeedItem> items,
        string provider,
        string model,
        CancellationToken cancellationToken)
    {
        var maxItems = _configuration.GetValue("RecallAgent:MaxItemsForPrompt", 10);
        var promptItems = items.Take(Math.Clamp(maxItems, 5, 15)).Select(i => new
        {
            drugName = i.DrugName,
            company = i.CompanyName,
            source = string.IsNullOrWhiteSpace(i.SourceName) ? "Manual" : i.SourceName,
            recallDate = i.RecallDate.ToString("yyyy-MM-dd"),
            recallLevel = i.RecallLevel,
            reason = Truncate(i.Reason, 180)
        });

        var promptData = JsonSerializer.Serialize(new
        {
            totalItemCount = items.Count,
            sourceCounts = items
                .GroupBy(i => string.IsNullOrWhiteSpace(i.SourceName) ? "Manual" : i.SourceName!)
                .ToDictionary(g => g.Key, g => g.Count()),
            latestItems = promptItems
        });

        const string systemPrompt =
            "You are PharmaCore's pharmacovigilance AI agent. Analyze only the recall data provided. " +
            "Do not invent drug names, companies, dates, or reasons. Return only valid JSON with exactly these keys: " +
            "summary (string), riskLevel (Low, Medium, or High), recommendedAction (string), and " +
            "highlightDrugNames (array of at most 3 drug names copied from the data). " +
            "Keep the summary under 80 words and mention the sources.";

        var userPrompt =
            "Analyze the latest medicine recall data below. Identify the most important recent recalls, " +
            "note the source of each important item, and recommend what the administrator should review first.\n\n" +
            promptData;

        var content = provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
            ? await CallOpenAiAsync(systemPrompt, userPrompt, model, cancellationToken)
            : await CallOllamaAsync(systemPrompt, userPrompt, model, cancellationToken);

        var json = ExtractJson(content);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        RecallAgentAiOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<RecallAgentAiOutput>(json, JsonOptions);
        }
        catch (JsonException)
        {
            output = SalvageAiOutput(json);
        }

        if (output == null || string.IsNullOrWhiteSpace(output.Summary))
        {
            return null;
        }

        output.HighlightDrugNames ??= new List<string>();
        return output;
    }

    private async Task<string> CallOllamaAsync(
        string systemPrompt,
        string userPrompt,
        string model,
        CancellationToken cancellationToken)
    {
        var baseUrl = (_configuration["RecallAgent:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/');
        var timeout = _configuration.GetValue("RecallAgent:TimeoutSeconds", 120);
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeout, 10, 300));

        var request = new
        {
            model,
            stream = false,
            format = "json",
            keep_alive = 0,
            options = new
            {
                temperature = 0.2,
                num_predict = 450,
                num_ctx = 2048,
                num_thread = Math.Max(1, Environment.ProcessorCount)
            },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        using var response = await client.PostAsJsonAsync($"{baseUrl}/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!doc.RootElement.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content))
        {
            throw new InvalidOperationException("Ollama returned an unexpected response.");
        }

        return content.GetString() ?? string.Empty;
    }

    private async Task<string> CallOpenAiAsync(
        string systemPrompt,
        string userPrompt,
        string model,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["RecallAgent:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("RecallAgent:ApiKey is not configured for the OpenAI provider.");
        }

        var baseUrl = (_configuration["RecallAgent:BaseUrl"] ?? "https://api.openai.com").TrimEnd('/');
        var endpoint = baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUrl}/chat/completions"
            : $"{baseUrl}/v1/chat/completions";

        var timeout = _configuration.GetValue("RecallAgent:TimeoutSeconds", 120);
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeout, 10, 300));

        var request = new
        {
            model,
            temperature = 0.2,
            max_tokens = 450,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!doc.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("OpenAI returned an unexpected response.");
        }

        var message = choices[0].GetProperty("message");
        return message.TryGetProperty("content", out var content)
            ? content.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string? ExtractJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLine = trimmed.IndexOf('\n');
            if (firstLine >= 0)
            {
                trimmed = trimmed[(firstLine + 1)..];
            }

            var fence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (fence >= 0)
            {
                trimmed = trimmed[..fence];
            }
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return start >= 0 && end > start ? trimmed[start..(end + 1)] : null;
    }

    private static void ApplyFallback(
        RecallAgentResult result,
        IReadOnlyList<RecallFeedItem> items,
        string message)
    {
        result.Success = true;
        result.AiUsed = false;
        result.Message = $"{message} {result.FetchedCount} recall item(s) were fetched and {result.SavedCount} saved.";
        result.RiskLevel = "Not assessed";
        result.Summary =
            $"Fetched {result.FetchedCount} recall item(s) from {FormatSources(result.SourceCounts)}. " +
            "Review the latest records and their source links below.";
        result.RecommendedAction = "Open the source links and verify the latest DDA and FDA recall notices.";
        result.Highlights = items.Take(5)
            .Select(i => new RecallAgentHighlight
            {
                DrugName = i.DrugName,
                Company = i.CompanyName,
                SourceName = string.IsNullOrWhiteSpace(i.SourceName) ? "Manual" : i.SourceName,
                RecallDate = i.RecallDate,
                Reason = string.IsNullOrWhiteSpace(i.Reason) ? "See source notice." : i.Reason
            })
            .ToList();
    }

    private static List<RecallAgentHighlight> MapHighlights(
        IEnumerable<string> drugNames,
        IReadOnlyList<RecallFeedItem> items)
    {
        var mapped = new List<RecallAgentHighlight>();

        foreach (var drugName in drugNames.Take(3))
        {
            if (string.IsNullOrWhiteSpace(drugName))
            {
                continue;
            }

            var match = items.FirstOrDefault(i =>
                i.DrugName.Equals(drugName, StringComparison.OrdinalIgnoreCase));

            mapped.Add(new RecallAgentHighlight
            {
                DrugName = string.IsNullOrWhiteSpace(match?.DrugName) ? drugName : match.DrugName,
                Company = match?.CompanyName,
                SourceName = match == null
                    ? null
                    : string.IsNullOrWhiteSpace(match.SourceName) ? "Manual" : match.SourceName,
                RecallDate = match?.RecallDate,
                Reason = string.IsNullOrWhiteSpace(match?.Reason) ? "See source notice." : match.Reason!
            });
        }

        return mapped;
    }

    private static RecallAgentAiOutput? SalvageAiOutput(string json)
    {
        var summary = ExtractJsonString(json, "summary");
        if (string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        return new RecallAgentAiOutput
        {
            Summary = summary,
            RiskLevel = ExtractJsonString(json, "riskLevel") ?? "Unknown",
            RecommendedAction = ExtractJsonString(json, "recommendedAction") ?? string.Empty
        };
    }

    private static string? ExtractJsonString(string json, string key)
    {
        var match = Regex.Match(
            json,
            $"\"{Regex.Escape(key)}\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"",
            RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string>($"\"{match.Groups[1].Value}\"");
        }
        catch (JsonException)
        {
            return match.Groups[1].Value;
        }
    }

    private static RecallAgentItem ToAgentItem(RecallFeedItem item)
    {
        return new RecallAgentItem
        {
            DrugName = item.DrugName,
            Company = item.CompanyName,
            SourceName = string.IsNullOrWhiteSpace(item.SourceName) ? "Manual" : item.SourceName,
            RecallDate = item.RecallDate,
            RecallLevel = item.RecallLevel,
            Reason = item.Reason,
            SourceUrl = item.SourceUrl
        };
    }

    private static string FormatSources(IReadOnlyDictionary<string, int> sourceCounts)
    {
        return sourceCounts.Count == 0
            ? "the configured sources"
            : string.Join(", ", sourceCounts.Select(kv => $"{kv.Key} ({kv.Value})"));
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }

    private static string NormalizeRiskLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Contains("class iii"))
        {
            return "Low";
        }

        if (normalized.Contains("class ii"))
        {
            return "Medium";
        }

        if (normalized.Contains("class i") || normalized.Contains("high"))
        {
            return "High";
        }

        if (normalized.Contains("medium") || normalized.Contains("moderate"))
        {
            return "Medium";
        }

        if (normalized.Contains("low"))
        {
            return "Low";
        }

        return "Unknown";
    }
}
