using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;

namespace Project.Controllers;

[AllowAnonymous]
public class PublicRecallsController : Controller
{
    private const int PageSize = 20;

    private readonly ApplicationDbContext _context;

    public PublicRecallsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? level, int page = 1)
    {
        page = Math.Max(1, page);

        var query = _context.DrugRecalls.AsNoTracking()
            .Include(r => r.Company)
            .Where(r => r.IsApproved);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(r =>
                r.DrugName.Contains(term) ||
                (r.Reason != null && r.Reason.Contains(term)) ||
                (r.Company != null && r.Company.Name.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            var selectedLevel = level.Trim();
            query = query.Where(r => r.RecallLevel == selectedLevel);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.RecallDate)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var model = await BuildModelAsync(items, page, total, q, level);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Pending(string? q, int page = 1)
    {
        page = Math.Max(1, page);

        var query = _context.DrugRecalls.AsNoTracking()
            .Include(r => r.Company)
            .Where(r => r.ApprovalStatus == "Pending" && !r.IsApproved);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(r =>
                r.DrugName.Contains(term) ||
                (r.Reason != null && r.Reason.Contains(term)) ||
                (r.Company != null && r.Company.Name.Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.SubmittedAt ?? r.RecallDate)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var model = await BuildModelAsync(items, page, total, q, null);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var recall = await _context.DrugRecalls.AsNoTracking()
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recall == null)
        {
            return NotFound();
        }

        if (!recall.IsApproved && recall.ApprovalStatus != "Pending")
        {
            return NotFound();
        }

        ViewBag.LastSyncAt = await GetLastSyncAsync();
        return View(recall);
    }

    private async Task<PublicRecallListViewModel> BuildModelAsync(
        List<DrugRecall> items,
        int page,
        int total,
        string? q,
        string? level)
    {
        return new PublicRecallListViewModel
        {
            Items = items,
            Page = page,
            PageSize = PageSize,
            TotalCount = total,
            Query = q,
            Level = level,
            LastSyncAt = await GetLastSyncAsync(),
            PendingCount = await _context.DrugRecalls.AsNoTracking()
                .CountAsync(r => r.ApprovalStatus == "Pending" && !r.IsApproved)
        };
    }

    private async Task<DateTime?> GetLastSyncAsync()
    {
        return await _context.RecallSyncLogs.AsNoTracking()
            .Where(l => l.Status == "Success" && l.CompletedAt != null)
            .OrderByDescending(l => l.CompletedAt)
            .Select(l => l.CompletedAt)
            .FirstOrDefaultAsync();
    }
}
