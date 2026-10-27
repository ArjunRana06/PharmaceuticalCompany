using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var model = new DashboardViewModel
        {
            CompanyCount = await _context.Companies.AsNoTracking().CountAsync(),
            DrugCount = await _context.Drugs.AsNoTracking().CountAsync(),
            RecallCount = await _context.DrugRecalls.AsNoTracking().CountAsync(r => r.IsApproved),
            NoticeCount = await _context.DdaNotices.AsNoTracking().CountAsync(),
            UserCount = await _context.Users.AsNoTracking().CountAsync(),
            NewDrugsThisMonth = await _context.Drugs.AsNoTracking()
                .CountAsync(d => d.RegistrationDate >= monthStart && d.RegistrationDate < nextMonth),
            RecallsThisMonth = await _context.DrugRecalls.AsNoTracking()
                .CountAsync(r => r.IsApproved && r.RecallDate >= monthStart && r.RecallDate < nextMonth)
        };

        model.RecentRecalls = await _context.DrugRecalls.AsNoTracking()
            .Where(r => r.IsApproved)
            .OrderByDescending(r => r.RecallDate)
            .Take(5)
            .Select(r => new DashboardRecall
            {
                Id = r.Id,
                DrugName = r.DrugName,
                BatchNumber = r.BatchNumber,
                Reason = r.Reason,
                RecallLevel = r.RecallLevel,
                RecallDate = r.RecallDate,
                CompanyName = r.Company != null ? r.Company.Name : "Unassigned"
            })
            .ToListAsync();

        model.RecentNotices = await _context.DdaNotices.AsNoTracking()
            .OrderByDescending(n => n.NoticeDate)
            .Take(5)
            .Select(n => new DashboardNotice
            {
                Id = n.Id,
                Title = n.Title,
                NoticeType = n.NoticeType,
                NoticeDate = n.NoticeDate
            })
            .ToListAsync();

        model.ExportCapableCount = await _context.Companies.AsNoTracking()
            .CountAsync(c => c.ExportCapability != null &&
                             (c.ExportCapability.StartsWith("Yes") || c.ExportCapability.StartsWith("Export-capable")));
        model.DomesticOnlyCount = await _context.Companies.AsNoTracking()
            .CountAsync(c => c.ExportCapability != null &&
                             c.ExportCapability.StartsWith("No public evidence"));
        model.DocumentedRecallEvents = await _context.Companies.AsNoTracking()
            .SumAsync(c => c.DocumentedRecallEvents);
        model.CategoryCount = await _context.DrugCategories.AsNoTracking().CountAsync();
        model.ProvinceCount = await _context.Companies.AsNoTracking()
            .Where(c => c.Province != null)
            .Select(c => c.Province!)
            .Distinct()
            .CountAsync();

        model.TopCompanies = await _context.Companies.AsNoTracking()
            .OrderByDescending(c => c.Drugs.Count)
            .Take(5)
            .Select(c => new DashboardCompany
            {
                Id = c.Id,
                Name = c.Name,
                Province = c.Province ?? "—",
                DrugCount = c.Drugs.Count
            })
            .ToListAsync();

        for (var i = 5; i >= 0; i--)
        {
            var start = monthStart.AddMonths(-i);
            var end = start.AddMonths(1);

            model.MonthlyActivity.Add(new DashboardMonth
            {
                Label = start.ToString("MMM"),
                DrugCount = await _context.Drugs.AsNoTracking().CountAsync(d => d.RegistrationDate >= start && d.RegistrationDate < end),
                RecallCount = await _context.DrugRecalls.AsNoTracking().CountAsync(r => r.IsApproved && r.RecallDate >= start && r.RecallDate < end)
            });
        }

        return View(model);
    }
}
