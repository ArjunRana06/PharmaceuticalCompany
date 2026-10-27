using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
public class CompanyController : Controller
{
    private readonly ApplicationDbContext _context;

    public CompanyController(ApplicationDbContext context)
    {
        _context = context;
    }

    // READ
    public async Task<IActionResult> Index()
    {
        var companies = await _context.Companies.AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(companies);
    }

    // CREATE - GET
    public IActionResult Create()
    {
        return View();
    }

    // CREATE - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Company company)
    {
        if (!ModelState.IsValid)
        {
            return View(company);
        }

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // EDIT - GET
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var company = await _context.Companies.FindAsync(id);

        if (company == null)
        {
            return NotFound();
        }

        return View(company);
    }

    // EDIT - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Company company)
    {
        if (id != company.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(company);
        }

        try
        {
            _context.Companies.Update(company);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CompanyExists(company.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // DELETE - GET confirmation
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == id);

        if (company == null)
        {
            return NotFound();
        }

        return View(company);
    }

    // DELETE - POST
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var company = await _context.Companies.FindAsync(id);

        if (company == null)
        {
            return NotFound();
        }

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private bool CompanyExists(int id)
    {
        return _context.Companies.Any(e => e.Id == id);
    }
}