namespace Project.Models;

/// <summary>
/// Official drug categories and sale / labelling rules
/// (Drugs Categorization Rules, 2043, Rule 10(2) and Schedule-4).
/// </summary>
public class DrugCategory
{
    public int Id { get; set; }

    /// <summary>Schedule-4 category letter (a, b, c).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Sub-category label, when the schedule splits the category.</summary>
    public string? SubCategory { get; set; }

    /// <summary>Drugs covered / definition of the category.</summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>Is a registered doctor's prescription required?</summary>
    public string? PrescriptionRequired { get; set; }

    /// <summary>Who may sell or dispense the drug.</summary>
    public string? WhoMaySell { get; set; }

    /// <summary>Mandatory label warning.</summary>
    public string? LabelWarning { get; set; }

    /// <summary>Legal reference for the rule.</summary>
    public string? LegalReference { get; set; }

    /// <summary>Display order inside the schedule.</summary>
    public int SortOrder { get; set; }
}
