using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Project.Models;
using Project.Services;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
public class RecallAgentController : Controller
{
    private readonly IRecallAgentService _agentService;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RecallAgentController> _logger;

    public RecallAgentController(
        IRecallAgentService agentService,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<RecallAgentController> logger)
    {
        _agentService = agentService;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.AgentEnabled = _configuration.GetValue("RecallAgent:Enabled", true);
        ViewBag.Provider = _configuration["RecallAgent:Provider"] ?? "Ollama";
        ViewBag.Model = _configuration["RecallAgent:Model"] ?? "qwen2.5-coder:3b";
        ViewBag.BaseUrl = _configuration["RecallAgent:BaseUrl"] ?? "http://localhost:11434";

        RecallAgentResult? result = null;
        var key = TempData["RecallAgentKey"] as string;
        if (!string.IsNullOrWhiteSpace(key))
        {
            _cache.TryGetValue(key, out result);
        }

        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        RecallAgentResult result;

        try
        {
            result = await _agentService.RunAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI recall agent run failed.");
            result = new RecallAgentResult
            {
                Message = "The AI recall agent failed. Check the application logs for details."
            };
        }

        var key = Guid.NewGuid().ToString("N");
        _cache.Set(key, result, TimeSpan.FromMinutes(15));
        TempData["RecallAgentKey"] = key;

        return RedirectToAction(nameof(Index));
    }
}
