namespace Project.Models;

public class PublicRecallListViewModel
{
    public IReadOnlyList<DrugRecall> Items { get; set; } = Array.Empty<DrugRecall>();

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public string? Query { get; set; }

    public string? Level { get; set; }

    public DateTime? LastSyncAt { get; set; }

    public int PendingCount { get; set; }
}
