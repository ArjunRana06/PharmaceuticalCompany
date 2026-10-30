namespace Project.Models;

public class DrugCategory
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public string? SubCategory { get; set; }

    public string Definition { get; set; } = string.Empty;

    public string? PrescriptionRequired { get; set; }

    public string? WhoMaySell { get; set; }

    public string? LabelWarning { get; set; }

    public string? LegalReference { get; set; }

    public int SortOrder { get; set; }
}
