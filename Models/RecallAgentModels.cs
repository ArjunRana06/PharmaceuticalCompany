namespace Project.Models;

public class RecallAgentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool AiUsed { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime RunAt { get; set; } = DateTime.Now;
    public int FetchedCount { get; set; }
    public int SavedCount { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Unknown";
    public string RecommendedAction { get; set; } = string.Empty;
    public List<RecallAgentHighlight> Highlights { get; set; } = new();
    public List<RecallAgentItem> Items { get; set; } = new();
    public Dictionary<string, int> SourceCounts { get; set; } = new();
}

public class RecallAgentHighlight
{
    public string DrugName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? SourceName { get; set; }
    public DateTime? RecallDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RecallAgentItem
{
    public string DrugName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? SourceName { get; set; }
    public DateTime RecallDate { get; set; }
    public string? RecallLevel { get; set; }
    public string? Reason { get; set; }
    public string? SourceUrl { get; set; }
}

public class RecallAgentAiOutput
{
    public string Summary { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Unknown";
    public string RecommendedAction { get; set; } = string.Empty;
    public List<string> HighlightDrugNames { get; set; } = new();
}
