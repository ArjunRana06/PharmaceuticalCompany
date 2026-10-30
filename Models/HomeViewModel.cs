namespace Project.Models;

public class HomeViewModel
{
    public int CompanyCount { get; set; }

    public int DrugCount { get; set; }

    public int RecallCount { get; set; }

    public int PendingRecallCount { get; set; }

    public DateTime? LastRecallSyncAt { get; set; }

    public int NoticeCount { get; set; }

    public int ExportCapableCount { get; set; }

    public int DocumentedRecallEvents { get; set; }

    public int ProvinceCount { get; set; }

    public List<HomeCompany> FeaturedCompanies { get; set; } = new List<HomeCompany>();

    public List<DashboardRecall> RecentRecalls { get; set; } = new List<DashboardRecall>();

    public List<DashboardRecall> RecentPendingRecalls { get; set; } = new List<DashboardRecall>();

    public List<DashboardNotice> RecentNotices { get; set; } = new List<DashboardNotice>();
}

public class HomeCompany
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Province { get; set; }

    public int DrugCount { get; set; }
}
