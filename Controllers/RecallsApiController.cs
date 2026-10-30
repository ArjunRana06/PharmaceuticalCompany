using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;

namespace Project.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/recalls")]
[Produces("application/json")]
public class RecallsApiController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly ApplicationDbContext _context;

    public RecallsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<RecallApiPage>> GetRecalls(
        string? q,
        string? level,
        string? source,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        var query = _context.DrugRecalls.AsNoTracking().Where(r => r.IsApproved);

        query = ApplySearch(query, q);

        if (!string.IsNullOrWhiteSpace(level))
        {
            var selectedLevel = level.Trim();
            query = query.Where(r => r.RecallLevel == selectedLevel);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            var selectedSource = source.Trim();
            query = selectedSource.Equals("Manual", StringComparison.OrdinalIgnoreCase)
                ? query.Where(r => r.SourceName == null)
                : query.Where(r => r.SourceName == selectedSource);
        }

        var result = await BuildPageAsync(query, page, pageSize);
        return Ok(result);
    }

    [HttpGet("pending")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<RecallApiPage>> GetPendingRecalls(
        string? q,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        var query = _context.DrugRecalls.AsNoTracking()
            .Where(r => r.ApprovalStatus == "Pending" && !r.IsApproved);

        query = ApplySearch(query, q);

        var result = await BuildPageAsync(query, page, pageSize, r => r.SubmittedAt ?? r.RecallDate);
        return Ok(result);
    }

    [HttpGet("status")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<RecallApiStatus>> GetStatus()
    {
        var sources = await _context.DrugRecalls.AsNoTracking()
            .Where(r => r.IsApproved)
            .GroupBy(r => r.SourceName ?? "Manual")
            .Select(g => new RecallApiSourceSummary
            {
                SourceName = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .ToListAsync();

        var result = new RecallApiStatus
        {
            LastSyncAt = await GetLastSyncAsync(),
            ApprovedCount = await _context.DrugRecalls.AsNoTracking().CountAsync(r => r.IsApproved),
            PendingCount = await _context.DrugRecalls.AsNoTracking()
                .CountAsync(r => r.ApprovalStatus == "Pending" && !r.IsApproved),
            Sources = sources
        };

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecallApiItem>> GetRecall(int id)
    {
        var recall = await _context.DrugRecalls.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RecallApiItem
            {
                Id = r.Id,
                DrugName = r.DrugName,
                Reason = r.Reason,
                BatchNumber = r.BatchNumber,
                RecallDate = r.RecallDate,
                RecallLevel = r.RecallLevel,
                SourceName = r.SourceName,
                SourceUrl = r.SourceUrl,
                ExternalId = r.ExternalId,
                Company = r.Company != null ? r.Company.Name : null,
                IsApproved = r.IsApproved,
                ApprovalStatus = r.ApprovalStatus,
                SubmittedAt = r.SubmittedAt,
                ApprovedAt = r.ApprovedAt
            })
            .FirstOrDefaultAsync();

        if (recall == null || (!recall.IsApproved && recall.ApprovalStatus != "Pending"))
        {
            return NotFound(new { message = "Recall not found." });
        }

        return Ok(recall);
    }

    private static IQueryable<DrugRecall> ApplySearch(IQueryable<DrugRecall> query, string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return query;
        }

        var term = q.Trim();
        return query.Where(r =>
            r.DrugName.Contains(term) ||
            (r.Reason != null && r.Reason.Contains(term)) ||
            (r.BatchNumber != null && r.BatchNumber.Contains(term)) ||
            (r.Company != null && r.Company.Name.Contains(term)));
    }

    private async Task<RecallApiPage> BuildPageAsync(
        IQueryable<DrugRecall> query,
        int page,
        int pageSize,
        System.Linq.Expressions.Expression<Func<DrugRecall, DateTime>>? orderBy = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await query.CountAsync();
        var ordered = orderBy == null
            ? query.OrderByDescending(r => r.RecallDate).ThenByDescending(r => r.Id)
            : query.OrderByDescending(orderBy).ThenByDescending(r => r.Id);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RecallApiItem
            {
                Id = r.Id,
                DrugName = r.DrugName,
                Reason = r.Reason,
                BatchNumber = r.BatchNumber,
                RecallDate = r.RecallDate,
                RecallLevel = r.RecallLevel,
                SourceName = r.SourceName,
                SourceUrl = r.SourceUrl,
                ExternalId = r.ExternalId,
                Company = r.Company != null ? r.Company.Name : null,
                IsApproved = r.IsApproved,
                ApprovalStatus = r.ApprovalStatus,
                SubmittedAt = r.SubmittedAt,
                ApprovedAt = r.ApprovedAt
            })
            .ToListAsync();

        return new RecallApiPage
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Items = items
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
