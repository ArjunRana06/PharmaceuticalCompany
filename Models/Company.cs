namespace Project.Models;

public class Company
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Province { get; set; }

    public string? District { get; set; }
    public string? City { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    /// <summary>Export capability / distribution outside Nepal (from the DDA dataset).</summary>
    public string? ExportCapability { get; set; }

    /// <summary>International recall search result (from the DDA dataset).</summary>
    public string? InternationalRecallRecords { get; set; }

    /// <summary>Primary source used for the distribution / recall information.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Number of documented recall events (dataset + additional DDA notices).</summary>
    public int DocumentedRecallEvents { get; set; }

    public ICollection<Drug> Drugs { get; set; } = new List<Drug>();

    public ICollection<DrugRecall> DrugRecalls { get; set; } = new List<DrugRecall>();
}
