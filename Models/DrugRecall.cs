using System.ComponentModel.DataAnnotations;

namespace Project.Models;

public class DrugRecall
{
    public int Id { get; set; }

    public string DrugName { get; set; } = string.Empty;

    public string? SideEffects { get; set; }

    public string? BatchNumber { get; set; }

    public string? Reason { get; set; }

    public DateTime RecallDate { get; set; }

    public int Year => RecallDate.Year;

    public string? RecallLevel { get; set; }

    public string? SourceUrl { get; set; }

    public int CompanyId { get; set; }

    public Company? Company { get; set; }

    // Approval workflow
    public bool IsApproved { get; set; } = false;

    public string ApprovalStatus { get; set; } = "Pending";

    public int? SubmittedByUserId { get; set; }

    public User? SubmittedByUser { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? ApprovedByUserId { get; set; }

    public User? ApprovedByUser { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }
}
