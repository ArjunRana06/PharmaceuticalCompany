using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using Project.Services;

namespace Project.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordService _passwordService;

    public HomeController(ApplicationDbContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel
        {
            CompanyCount = await _context.Companies.AsNoTracking().CountAsync(),
            DrugCount = await _context.Drugs.AsNoTracking().CountAsync(),
            RecallCount = await _context.DrugRecalls.AsNoTracking().CountAsync(r => r.IsApproved),
            PendingRecallCount = await _context.DrugRecalls.AsNoTracking()
                .CountAsync(r => r.ApprovalStatus == "Pending" && !r.IsApproved),
            LastRecallSyncAt = await _context.RecallSyncLogs.AsNoTracking()
                .Where(l => l.Status == "Success" && l.CompletedAt != null)
                .OrderByDescending(l => l.CompletedAt)
                .Select(l => l.CompletedAt)
                .FirstOrDefaultAsync(),
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

        model.RecentPendingRecalls = await _context.DrugRecalls.AsNoTracking()
            .Where(r => r.ApprovalStatus == "Pending" && !r.IsApproved)
            .OrderByDescending(r => r.SubmittedAt ?? r.RecallDate)
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginUser(LoginModel model, bool remember = false)
    {
        model.Email = model.Email?.Trim() ?? string.Empty;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !_passwordService.Verify(user, model.Password, out var needsRehash))
        {
            ViewBag.Error = "Invalid email or password.";
            return View("Login");
        }

        if (needsRehash)
        {
            user.Password = _passwordService.Hash(user, model.Password);
            await _context.SaveChangesAsync();
        }

        var role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = remember
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

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
