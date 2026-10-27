namespace Project.Models;

public class Drug
{
    public int Id { get; set; }

    public string DrugName { get; set; } = string.Empty;

    public string? GenericName { get; set; }

    public string? DosageForm { get; set; }

    public string? Strength { get; set; }

    public string? RegistrationNumber { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public int CompanyId { get; set; }

    public Company? Company { get; set; }
}