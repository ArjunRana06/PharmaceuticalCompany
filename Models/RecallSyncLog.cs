using System.ComponentModel.DataAnnotations;

namespace Project.Models;

public class RecallSyncLog
{
    public int Id { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Running";

    public int ItemsFetched { get; set; }

    public int ItemsSaved { get; set; }

    [MaxLength(1000)]
    public string? Message { get; set; }
}
