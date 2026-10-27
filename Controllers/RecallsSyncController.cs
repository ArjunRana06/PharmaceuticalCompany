using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Services;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
public class RecallsSyncController : Controller
{
    private readonly IRecallSyncService _syncService;
    private readonly ILogger<RecallsSyncController> _logger;

    public RecallsSyncController(IRecallSyncService syncService, ILogger<RecallsSyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
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
