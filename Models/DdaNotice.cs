namespace Project.Models;

public class DdaNotice
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime NoticeDate { get; set; }

    public string? NoticeType { get; set; }

    public string? SourceUrl { get; set; }
}