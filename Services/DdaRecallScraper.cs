using System.Net;
using System.Text.RegularExpressions;
using Project.Models;

namespace Project.Services;

public class DdaRecallScraper
{
    private static readonly Regex ContentLinkRegex = new(
        "<a[^>]+href=\"(?<href>/content/(?<id>\\d+)/[^\"]+)\"[^>]*>(?<text>.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);
    private static readonly Regex BsDateRegex = new(
        "(?<year>20\\d{2})\\s*[/\\-.]\\s*(?<month>\\d{1,2})\\s*[/\\-.]\\s*(?<day>\\d{1,2})",
        RegexOptions.Compiled);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DdaRecallScraper> _logger;

    public DdaRecallScraper(IHttpClientFactory httpClientFactory, ILogger<DdaRecallScraper> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RecallFeedItem>> FetchLatestAsync(
        string categoryUrl,
        int maxItems,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryUrl))
        {
            return Array.Empty<RecallFeedItem>();
        }

        var html = await GetStringWithRetryAsync(categoryUrl, cancellationToken);
        if (string.IsNullOrEmpty(html))
        {
            return Array.Empty<RecallFeedItem>();
        }

        var items = new List<RecallFeedItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in ContentLinkRegex.Matches(html))
        {
            var id = match.Groups["id"].Value;
            if (!seen.Add(id))
            {
                continue;
            }

            var href = WebUtility.HtmlDecode(match.Groups["href"].Value);
            var rawText = match.Groups["text"].Value;
            var title = WhitespaceRegex.Replace(
                WebUtility.HtmlDecode(TagRegex.Replace(rawText, " ")), " ").Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (!title.Contains("recall", StringComparison.OrdinalIgnoreCase) &&
                !title.Contains("फिर्ता", StringComparison.Ordinal))
            {
                continue;
            }

            var recallDate = ParseBsDate(title) ?? DateTime.UtcNow.Date;
            var sourceUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? href
                : "https://dda.gov.np" + href;

            items.Add(new RecallFeedItem
            {
                DrugName = Truncate(title, 200),
                BatchNumber = null,
                Reason = "Official DDA drug recall notice. Open the source link to view the notice document/image.",
                RecallDate = recallDate,
                RecallLevel = null,
                SourceUrl = Truncate(sourceUrl, 500),
                SourceName = "DDA Nepal",
                ExternalId = "dda-" + id,
                CompanyName = null
            });

            if (items.Count >= maxItems)
            {
                break;
            }
        }

        _logger.LogInformation("DDA recall scraper found {Count} recall notice(s).", items.Count);
        return items;
    }

    private async Task<string?> GetStringWithRetryAsync(string url, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PharmaCore-RecallSync/1.0");

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < 3)
            {
                _logger.LogWarning(ex, "DDA fetch attempt {Attempt} failed for {Url}. Retrying.", attempt, url);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch DDA recall page {Url}.", url);
                return null;
            }
        }

        return null;
    }

    private static DateTime? ParseBsDate(string title)
    {
        var normalized = NormalizeDigits(title);
        var match = BsDateRegex.Match(normalized);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["year"].Value, out var bsYear) ||
            !int.TryParse(match.Groups["month"].Value, out var bsMonth) ||
            !int.TryParse(match.Groups["day"].Value, out var bsDay))
        {
            return null;
        }

        if (bsMonth is < 1 or > 12 || bsDay is < 1 or > 32 || bsYear is < 2070 or > 2100)
        {
            return null;
        }

        if (bsYear == 2082)
        {
            var starts = new[]
            {
                new DateTime(2025, 4, 14), new DateTime(2025, 5, 15), new DateTime(2025, 6, 15),
                new DateTime(2025, 7, 17), new DateTime(2025, 8, 17), new DateTime(2025, 9, 17),
                new DateTime(2025, 10, 18), new DateTime(2025, 11, 17), new DateTime(2025, 12, 16),
                new DateTime(2026, 1, 15), new DateTime(2026, 2, 13), new DateTime(2026, 3, 15)
            };
            return starts[bsMonth - 1].AddDays(bsDay - 1);
        }

        var newYear = new DateTime(bsYear - 57, 4, 14);
        return newYear.AddDays((bsMonth - 1) * 30.44 + (bsDay - 1)).Date;
    }

    private static string NormalizeDigits(string input)
    {
        var map = new Dictionary<char, char>
        {
            ['०'] = '0',
            ['१'] = '1',
            ['२'] = '2',
            ['३'] = '3',
            ['४'] = '4',
            ['५'] = '5',
            ['६'] = '6',
            ['७'] = '7',
            ['८'] = '8',
            ['९'] = '9'
        };

        return new string(input.Select(c => map.TryGetValue(c, out var digit) ? digit : c).ToArray());
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value.Substring(0, maxLength);
}
