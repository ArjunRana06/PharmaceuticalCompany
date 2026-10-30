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

    public string? ExportCapability { get; set; }

    public string? InternationalRecallRecords { get; set; }

    public string? SourceUrl { get; set; }

    public int DocumentedRecallEvents { get; set; }

    public ICollection<Drug> Drugs { get; set; } = new List<Drug>();

    public ICollection<DrugRecall> DrugRecalls { get; set; } = new List<DrugRecall>();
}
