namespace Project.Models;

public class DashboardViewModel
{
    public int CompanyCount { get; set; }

    public int DrugCount { get; set; }

    public int RecallCount { get; set; }

    public int NoticeCount { get; set; }

    public int UserCount { get; set; }

    public int NewDrugsThisMonth { get; set; }

    public int RecallsThisMonth { get; set; }

    public int ExportCapableCount { get; set; }

    public int DomesticOnlyCount { get; set; }

    public int DocumentedRecallEvents { get; set; }

    public int CategoryCount { get; set; }

    public int ProvinceCount { get; set; }

    public List<DashboardRecall> RecentRecalls { get; set; } = new List<DashboardRecall>();

    public List<DashboardNotice> RecentNotices { get; set; } = new List<DashboardNotice>();

    public List<DashboardCompany> TopCompanies { get; set; } = new List<DashboardCompany>();

    public List<DashboardMonth> MonthlyActivity { get; set; } = new List<DashboardMonth>();
}

public class DashboardRecall
{
    public int Id { get; set; }

    public string DrugName { get; set; } = string.Empty;

    public string? BatchNumber { get; set; }

    public string? Reason { get; set; }

    public string? RecallLevel { get; set; }

    public DateTime RecallDate { get; set; }

    public string CompanyName { get; set; } = string.Empty;
}

public class DashboardNotice
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? NoticeType { get; set; }

    public DateTime NoticeDate { get; set; }
}

public class DashboardCompany
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public int DrugCount { get; set; }
}

public class DashboardMonth
{
    public string Label { get; set; } = string.Empty;

    public int DrugCount { get; set; }

    public int RecallCount { get; set; }
}
