using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
public class DrugCategoriesController : Controller
{
    private readonly ApplicationDbContext _context;

    public DrugCategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _context.DrugCategories
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return View(model);
    }
}
