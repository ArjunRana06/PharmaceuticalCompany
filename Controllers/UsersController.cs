using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using Project.Services;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordService _passwordService;

    public UsersController(ApplicationDbContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await GetUsersAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User user)
    {
        user.Name = user.Name?.Trim() ?? string.Empty;
        user.Email = user.Email?.Trim() ?? string.Empty;
        user.Password ??= string.Empty;

        if (user.Password.Length < 8)
        {
            ModelState.AddModelError(nameof(Project.Models.User.Password), "Password must be at least 8 characters.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            ModelState.AddModelError(nameof(Project.Models.User.Email), "A user with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelErrors();
            return View("Index", await GetUsersAsync());
        }

        user.Password = _passwordService.Hash(user, user.Password);
        user.Role = "User";
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "User created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(User user)
    {
        var existingUser = await _context.Users.FindAsync(user.Id);
        if (existingUser == null)
        {
            return NotFound();
        }

        user.Name = user.Name?.Trim() ?? string.Empty;
        user.Email = user.Email?.Trim() ?? string.Empty;

        if (await _context.Users.AnyAsync(u => u.Id != user.Id && u.Email == user.Email))
        {
            ModelState.AddModelError(nameof(Project.Models.User.Email), "A user with this email already exists.");
        }

        if (!string.IsNullOrWhiteSpace(user.Password) && user.Password.Length < 8)
        {
            ModelState.AddModelError(nameof(Project.Models.User.Password), "Password must be at least 8 characters.");
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = GetModelErrors();
            return View("Index", await GetUsersAsync());
        }

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;

        if (!string.IsNullOrWhiteSpace(user.Password))
        {
            existingUser.Password = _passwordService.Hash(existingUser, user.Password);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "User updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Admin user cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }

    private Task<List<User>> GetUsersAsync()
    {
        return _context.Users.AsNoTracking()
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    private string GetModelErrors()
    {
        return string.Join(" ", ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct());
    }
}
