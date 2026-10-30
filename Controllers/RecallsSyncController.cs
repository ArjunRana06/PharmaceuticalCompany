using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Services;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
public class RecallsSyncController : Controller
{
    private readonly IRecallSyncService _syncService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RecallsSyncController> _logger;

    public RecallsSyncController(
        IRecallSyncService syncService,
        ApplicationDbContext context,
        ILogger<RecallsSyncController> logger)
    {
        _syncService = syncService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Logs = await _context.RecallSyncLogs.AsNoTracking()
            .OrderByDescending(l => l.StartedAt)
            .Take(10)
            .ToListAsync();

        ViewBag.RecentSynced = await _context.DrugRecalls.AsNoTracking()
            .Include(r => r.Company)
            .Where(r => r.SourceName != null)
            .OrderByDescending(r => r.Id)
            .Take(10)
            .ToListAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncNow()
    {
        try
        {
            var count = await _syncService.SyncLatestAsync();
            TempData["Message"] = $"Synced {count} recall item(s) from public feed.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual recall sync failed");
            TempData["Error"] = "Sync failed. Check logs for details.";
        }

        return RedirectToAction("Index");
    }
}
