using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using System.Security.Claims;

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

    private bool IsAdmin => CurrentUserRole == "Admin";

    // =========================
    // READ - Basic list with cascading search (approved only)
    // =========================
    [HttpGet]
    public async Task<IActionResult> Index(string? drug, int? year, DateTime? recallDate)
    {
        var query = _context.DrugRecalls.AsNoTracking()
            .Where(r => r.IsApproved)
            .AsQueryable();

        if (!IsAdmin && CurrentUserId.HasValue)
        {
            query = query.Where(r => r.SubmittedByUserId == CurrentUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(drug))
        {
            query = query.Where(r => r.DrugName.Contains(drug.Trim()));
        }

        if (year.HasValue)
        {
            query = query.Where(r => r.RecallDate.Year == year.Value);
        }

        if (recallDate.HasValue)
        {
            query = query.Where(r => r.RecallDate.Date == recallDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(drug) || year.HasValue || recallDate.HasValue)
        {
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
            ViewBag.ShowAll = false;
            return View(recalls);
        }
        else
        {
            var allApproved = await query
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
            ViewBag.ShowAll = true;
            return View(allApproved);
        }
    }

    // =========================
    // CASCADING - AJAX endpoints
    // =========================
    [HttpGet]
    public async Task<IActionResult> GetDrugs()
    {
        var query = _context.DrugRecalls.AsNoTracking()
            .Where(r => r.IsApproved);
        if (!IsAdmin && CurrentUserId.HasValue)
        {
            query = query.Where(r => r.SubmittedByUserId == CurrentUserId.Value);
        }
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
            return Json(new List<int>());

        var drugName = drug.Trim();
        var query = _context.DrugRecalls.AsNoTracking()
            .Where(r => r.IsApproved && r.DrugName == drugName);
        if (!IsAdmin && CurrentUserId.HasValue)
        {
            query = query.Where(r => r.SubmittedByUserId == CurrentUserId.Value);
        }
        var years = await query
            .Select(r => r.RecallDate.Year)
            .ToListAsync();
        var result = years.Distinct().OrderBy(y => y).ToList();
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetRecallDates(string drug, int year)
    {
        if (string.IsNullOrWhiteSpace(drug))
            return Json(new List<object>());

        var drugName = drug.Trim();
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);
        var query = _context.DrugRecalls.AsNoTracking()
            .Where(r => r.IsApproved && r.DrugName == drugName && r.RecallDate >= start && r.RecallDate < end);
        if (!IsAdmin && CurrentUserId.HasValue)
        {
            query = query.Where(r => r.SubmittedByUserId == CurrentUserId.Value);
        }
        var dates = await query.ToListAsync();
        var result = dates
            .Select(r => new { date = r.RecallDate.ToString("yyyy-MM-dd"), label = r.RecallDate.ToString("dd MMM yyyy") })
            .Distinct()
            .OrderBy(d => d.date)
            .ToList();
        return Json(result);
    }

    // =========================
    // READ - Full data (Details)
    // =========================
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var recall = await _context.DrugRecalls
            .Include(r => r.Company)
            .Include(r => r.SubmittedByUser)
            .Include(r => r.ApprovedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recall == null)
            return NotFound();

        if (!recall.IsApproved && !IsAdmin && CurrentUserId != recall.SubmittedByUserId)
            return Forbid();

        return View(recall);
    }

    // =========================
    // CREATE - Level 1 submits for approval (pending)
    // =========================
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
            return View(recall);

        var userId = CurrentUserId;
        if (userId == null)
            return Challenge();

        recall.SubmittedByUserId = userId;
        recall.SubmittedAt = DateTime.Now;
        recall.IsApproved = false;
        recall.ApprovalStatus = "Pending";

        _context.DrugRecalls.Add(recall);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Your recall submission has been sent for approval.";
        return RedirectToAction(nameof(Index));
    }

    // =========================
    // UPDATE - Level 1 can edit their own pending submissions
    // =========================
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var recall = await _context.DrugRecalls.FindAsync(id);
        if (recall == null)
            return NotFound();

        if (!IsAdmin && CurrentUserId != recall.SubmittedByUserId)
            return Forbid();

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
            return NotFound();

        var existing = await _context.DrugRecalls.FindAsync(id);
        if (existing == null)
            return NotFound();

        if (!IsAdmin && CurrentUserId != existing.SubmittedByUserId)
            return Forbid();

        if (existing.ApprovalStatus != "Pending" && !IsAdmin)
        {
            TempData["Error"] = "Only pending submissions can be updated.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
            return View(recall);

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

    // =========================
    // DELETE - Level 1 can delete their own pending submissions
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var recall = await _context.DrugRecalls.FindAsync(id);
        if (recall == null)
            return NotFound();

        if (!IsAdmin && CurrentUserId != recall.SubmittedByUserId)
            return Forbid();

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

    // =========================
    // LEVEL 2 ADMIN - Pending approvals
    // =========================
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Pending()
    {
        var pending = await _context.DrugRecalls.AsNoTracking()
            .Include(r => r.Company)
            .Include(r => r.SubmittedByUser)
            .Where(r => r.ApprovalStatus == "Pending")
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync();
        return View(pending);
    }

    // =========================
    // LEVEL 2 ADMIN - Approve
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var recall = await _context.DrugRecalls.FindAsync(id);
        if (recall == null)
            return NotFound();

        recall.IsApproved = true;
        recall.ApprovalStatus = "Approved";
        recall.ApprovedByUserId = CurrentUserId;
        recall.ApprovedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Recall approved and added to the main database.";
        return RedirectToAction(nameof(Pending));
    }

    // =========================
    // LEVEL 2 ADMIN - Reject
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id, string? rejectionReason)
    {
        var recall = await _context.DrugRecalls.FindAsync(id);
        if (recall == null)
            return NotFound();

        recall.ApprovalStatus = "Rejected";
        recall.RejectionReason = rejectionReason;
        recall.ApprovedByUserId = CurrentUserId;
        recall.ApprovedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Recall rejected.";
        return RedirectToAction(nameof(Pending));
    }
}
