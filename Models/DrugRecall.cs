using System.ComponentModel.DataAnnotations;

namespace Project.Models;

public class DrugRecall
{
    public int Id { get; set; }

    [Required]
    public string DrugName { get; set; } = string.Empty;

    public string? SideEffects { get; set; }

    public string? BatchNumber { get; set; }

    public string? Reason { get; set; }

    public DateTime RecallDate { get; set; }

    public int Year => RecallDate.Year;

    public string? RecallLevel { get; set; }

    [MaxLength(500)]
    public string? SourceUrl { get; set; }

    [MaxLength(50)]
    public string? SourceName { get; set; }

    [MaxLength(100)]
    public string? ExternalId { get; set; }

    public int? CompanyId { get; set; }

    public Company? Company { get; set; }

    public bool IsApproved { get; set; }

    public string ApprovalStatus { get; set; } = "Pending";

    public int? SubmittedByUserId { get; set; }

    public User? SubmittedByUser { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? ApprovedByUserId { get; set; }

    public User? ApprovedByUser { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }
}
