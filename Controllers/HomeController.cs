using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Project.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel
        {
            CompanyCount = await _context.Companies.AsNoTracking().CountAsync(),
            DrugCount = await _context.Drugs.AsNoTracking().CountAsync(),
            RecallCount = await _context.DrugRecalls.AsNoTracking().CountAsync(r => r.IsApproved),
            NoticeCount = await _context.DdaNotices.AsNoTracking().CountAsync(),
            ExportCapableCount = await _context.Companies.AsNoTracking()
                .CountAsync(c => c.ExportCapability != null &&
                                 (c.ExportCapability.StartsWith("Yes") || c.ExportCapability.StartsWith("Export-capable"))),
            DocumentedRecallEvents = await _context.Companies.AsNoTracking().SumAsync(c => c.DocumentedRecallEvents),
            ProvinceCount = await _context.Companies.AsNoTracking()
                .Where(c => c.Province != null)
                .Select(c => c.Province!)
                .Distinct()
                .CountAsync()
        };

        model.FeaturedCompanies = await _context.Companies.AsNoTracking()
            .OrderByDescending(c => c.Drugs.Count)
            .Take(5)
            .Select(c => new HomeCompany
            {
                Id = c.Id,
                Name = c.Name,
                Province = c.Province,
                DrugCount = c.Drugs.Count
            })
            .ToListAsync();

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

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index");
    }

    [HttpPost]
public async Task<IActionResult> LoginUser(LoginModel model)
{
    var user = await _context.Users
        .FirstOrDefaultAsync(u =>
            u.Email == model.Email &&
            u.Password == model.Password);

    if (user == null)
    {
        ViewBag.Error = "Invalid email or password.";
        return View("Login");
    }

    var role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Name),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, role)
    };

    var identity = new ClaimsIdentity(
        claims,
        CookieAuthenticationDefaults.AuthenticationScheme);

    var principal = new ClaimsPrincipal(identity);

    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal);

    if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
    {
        return RedirectToAction("Index", "Dashboard");
    }

    return RedirectToAction("Index", "DrugRecalls");
}


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
