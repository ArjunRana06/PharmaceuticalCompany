using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
public class DdaNoticesController : Controller
{
    private readonly ApplicationDbContext _context;

    public DdaNoticesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? type)
    {
        var notices = _context.DdaNotices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            notices = notices.Where(n =>
                n.Title.Contains(term) ||
                (n.Description != null && n.Description.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            notices = notices.Where(n => n.NoticeType == type);
        }

        var model = await notices
            .OrderByDescending(n => n.NoticeDate)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.Types = await _context.DdaNotices.AsNoTracking()
            .Where(n => n.NoticeType != null)
            .Select(n => n.NoticeType!)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        return View(model);
    }
}
