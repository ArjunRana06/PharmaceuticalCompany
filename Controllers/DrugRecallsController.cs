using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;

namespace Project.Controllers;

[Authorize]
public class DrugRecallsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DrugRecallsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    private bool IsAdmin => string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase);

    private bool Owns(DrugRecall recall) =>
        CurrentUserId is int userId && recall.SubmittedByUserId == userId;

    [HttpGet]
    public async Task<IActionResult> Index(string? drug, int? year, DateTime? recallDate)
    {
        var query = ApplyAccessFilter(_context.DrugRecalls.AsNoTracking()
            .Where(r => r.IsApproved));

        if (!string.IsNullOrWhiteSpace(drug))
        {
            var term = drug.Trim();
            query = query.Where(r => r.DrugName.Contains(term));
        }

        if (year.HasValue)
        {
            query = query.Where(r => r.RecallDate.Year == year.Value);
        }

        if (recallDate.HasValue)
        {
            query = query.Where(r => r.RecallDate.Date == recallDate.Value.Date);
        }

        var recalls = await query
            .OrderByDescending(r => r.RecallDate)
            .Select(r => new
            {
                r.Id,
                r.DrugName,
                r.BatchNumber,
                r.RecallDate,
                r.RecallLevel,
                r.Reason,
                CompanyName = r.Company != null ? r.Company.Name : "Unassigned"
            })
            .ToListAsync();

        ViewBag.Drug = drug;
        ViewBag.Year = year;
        ViewBag.RecallDate = recallDate;
        ViewBag.ShowAll = string.IsNullOrWhiteSpace(drug) && !year.HasValue && !recallDate.HasValue;

        return View(recalls);
    }

    [HttpGet]
    public async Task<IActionResult> GetDrugs()
    {
        var query = ApplyAccessFilter(_context.DrugRecalls.AsNoTracking()
            .Where(r => r.IsApproved));

        var drugs = await query
            .Select(r => r.DrugName)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        return Json(drugs);
    }

    [HttpGet]
    public async Task<IActionResult> GetYears(string drug)
    {
        if (string.IsNullOrWhiteSpace(drug))
        {
            return Json(Array.Empty<int>());
        }

        var drugName = drug.Trim();
        var query = ApplyAccessFilter(_context.DrugRecalls.AsNoTracking()
            .Where(r => r.IsApproved && r.DrugName == drugName));

        var years = await query
            .Select(r => r.RecallDate.Year)
            .ToListAsync();

        return Json(years.Distinct().OrderBy(y => y));
    }

    [HttpGet]
    public async Task<IActionResult> GetRecallDates(string drug, int year)
    {
        if (string.IsNullOrWhiteSpace(drug) || year is < 1900 or > 2200)
        {
            return Json(Array.Empty<object>());
        }

        var drugName = drug.Trim();
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);
        var query = ApplyAccessFilter(_context.DrugRecalls.AsNoTracking()
            .Where(r => r.IsApproved && r.DrugName == drugName && r.RecallDate >= start && r.RecallDate < end));

        var dates = await query
            .Select(r => r.RecallDate)
            .ToListAsync();

        var result = dates
            .Select(date => new
            {
                date = date.ToString("yyyy-MM-dd"),
                label = date.ToString("dd MMM yyyy")
            })
            .Distinct()
            .OrderBy(d => d.date);

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var recall = await _context.DrugRecalls.AsNoTracking()
            .Include(r => r.Company)
            .Include(r => r.SubmittedByUser)
            .Include(r => r.ApprovedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recall == null)
        {
            return NotFound();
        }

        if (!recall.IsApproved && !IsAdmin && !Owns(recall))
        {
            return Forbid();
        }

        return View(recall);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DrugRecall recall)
    {
        if (!ModelState.IsValid)
        {
            return View(recall);
        }

        var userId = CurrentUserId;
        if (userId == null)
        {
            return Challenge();
        }

        recall.SubmittedByUserId = userId;
        recall.SubmittedAt = DateTime.Now;
        recall.IsApproved = false;
        recall.ApprovalStatus = "Pending";
        recall.SourceName = null;
        recall.ExternalId = null;

        _context.DrugRecalls.Add(recall);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Your recall submission has been sent for approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var recall = await _context.DrugRecalls.FindAsync(id);
        if (recall == null)
        {
            return NotFound();
        }

        if (!IsAdmin && !Owns(recall))
        {
            return Forbid();
        }

        if (recall.ApprovalStatus != "Pending" && !IsAdmin)
        {
            TempData["Error"] = "Only pending submissions can be edited.";
            return RedirectToAction(nameof(Index));
        }

        return View(recall);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DrugRecall recall)
    {
        if (id != recall.Id)
        {
            return NotFound();
        }

        var existing = await _context.DrugRecalls.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        if (!IsAdmin && !Owns(existing))
        {
            return Forbid();
        }

        if (existing.ApprovalStatus != "Pending" && !IsAdmin)
        {
            TempData["Error"] = "Only pending submissions can be updated.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return View(recall);
        }

        existing.DrugName = recall.DrugName;
        existing.SideEffects = recall.SideEffects;
        existing.BatchNumber = recall.BatchNumber;
        existing.Reason = recall.Reason;
        existing.RecallDate = recall.RecallDate;
        existing.RecallLevel = recall.RecallLevel;
        existing.SourceUrl = recall.SourceUrl;
        existing.CompanyId = recall.CompanyId;

        if (!IsAdmin)
        {
            existing.SubmittedAt = DateTime.Now;
            existing.ApprovalStatus = "Pending";
            existing.IsApproved = false;
            existing.ApprovedByUserId = null;
            existing.ApprovedAt = null;
            existing.RejectionReason = null;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = IsAdmin ? "Recall updated." : "Your update has been submitted for approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var recall = await _context.DrugRecalls.FindAsync(id);
        if (recall == null)
        {
            return NotFound();
        }

        if (!IsAdmin && !Owns(recall))
        {
            return Forbid();
        }

        if (recall.ApprovalStatus != "Pending" && !IsAdmin)
        {
            TempData["Error"] = "Only pending submissions can be deleted.";
            return RedirectToAction(nameof(Index));
        }

        _context.DrugRecalls.Remove(recall);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Recall deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Pending()
    {
        var pending = await _context.DrugRecalls.AsNoTracking()
            .Include(r => r.Company)
            .Include(r => r.SubmittedByUser)
            .Where(r => r.ApprovalStatus == "Pending" && !r.IsApproved)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();

        return View(pending);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var recall = await _context.DrugRecalls.FindAsync(id);
        if (recall == null)
        {
            return NotFound();
        }

        recall.IsApproved = true;
        recall.ApprovalStatus = "Approved";
        recall.ApprovedByUserId = CurrentUserId;
        recall.ApprovedAt = DateTime.Now;
        recall.RejectionReason = null;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Recall approved and added to the main database.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id, string? rejectionReason)
    {
        var recall = await _context.DrugRecalls.FindAsync(id);
        if (recall == null)
        {
            return NotFound();
        }

        recall.IsApproved = false;
        recall.ApprovalStatus = "Rejected";
        recall.RejectionReason = rejectionReason?.Trim();
        recall.ApprovedByUserId = CurrentUserId;
        recall.ApprovedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Recall rejected.";
        return RedirectToAction(nameof(Pending));
    }

    private IQueryable<DrugRecall> ApplyAccessFilter(IQueryable<DrugRecall> query)
    {
        if (IsAdmin)
        {
            return query;
        }

        return CurrentUserId is int userId
            ? query.Where(r => r.SubmittedByUserId == userId)
            : query.Where(_ => false);
    }
}
