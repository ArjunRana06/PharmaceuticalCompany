using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
public class DrugsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DrugsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? form)
    {
        var drugs = _context.Drugs
            .Include(d => d.Company)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            drugs = drugs.Where(d =>
                d.DrugName.Contains(term) ||
                (d.GenericName != null && d.GenericName.Contains(term)) ||
                (d.Company != null && d.Company.Name.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(form))
        {
            drugs = drugs.Where(d => d.DosageForm == form);
        }

        var model = await drugs.AsNoTracking()
            .OrderBy(d => d.DrugName)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Form = form;
        ViewBag.Forms = await _context.Drugs.AsNoTracking()
            .Where(d => d.DosageForm != null)
            .Select(d => d.DosageForm!)
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync();

        return View(model);
    }
}
