namespace Project.Models;

public class RecallFeedItem
{
    public string DrugName { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string? Reason { get; set; }
    public DateTime RecallDate { get; set; }
    public string? RecallLevel { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceName { get; set; }
    public string? ExternalId { get; set; }
    public string? CompanyName { get; set; }
    public string? SideEffects { get; set; }
}
