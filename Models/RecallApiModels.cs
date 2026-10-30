namespace Project.Models;

public class RecallApiItem
{
    public int Id { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime RecallDate { get; set; }
    public string? RecallLevel { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public string? ExternalId { get; set; }
    public string? Company { get; set; }
    public bool IsApproved { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class RecallApiPage
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public IReadOnlyList<RecallApiItem> Items { get; set; } = Array.Empty<RecallApiItem>();
}

public class RecallApiSourceSummary
{
    public string SourceName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecallApiStatus
{
    public DateTime? LastSyncAt { get; set; }
    public int ApprovedCount { get; set; }
    public int PendingCount { get; set; }
    public IReadOnlyList<RecallApiSourceSummary> Sources { get; set; } = Array.Empty<RecallApiSourceSummary>();
}
