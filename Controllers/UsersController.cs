using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using System.Net.Http.Json;

namespace Project.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{

private readonly ApplicationDbContext _context;
private readonly IHttpClientFactory _httpClientFactory;

public UsersController(
    ApplicationDbContext context,
    IHttpClientFactory httpClientFactory)
{
    _context = context;
    _httpClientFactory = httpClientFactory;
}

    // =========================
    // READ - Display all users
    // =========================
     
[HttpGet]
public async Task<IActionResult> Index()
{
    var users = await _context.Users.AsNoTracking().ToListAsync();
    return View(users);
}


[HttpGet]
public async Task<IActionResult> FromApi()
{
    var client = _httpClientFactory.CreateClient();

    try
    {
        var users = await client.GetFromJsonAsync<List<ApiUser>>(
            "http://localhost:5275/api/user"
        );

        return Ok(users ?? new List<ApiUser>());
        // return View(users ?? new List<ApiUser>());
    }
    catch (HttpRequestException)
    {
        return StatusCode(503, new
        {
            message = "Could not connect to the User API."
        });
    }
}


    // =========================
    // CREATE - Add user
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User user)
    {
        if (!ModelState.IsValid)
        {
            var users = await _context.Users.AsNoTracking().ToListAsync();

            return View("Index", users);
        }

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // UPDATE - Edit user
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(User user)
    {
        if (!ModelState.IsValid)
        {
            var users = await _context.Users.AsNoTracking().ToListAsync();

            return View("Index", users);
        }

        var existingUser = await _context.Users
            .FindAsync(user.Id);

        if (existingUser == null)
        {
            return NotFound();
        }

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;
        existingUser.Password = user.Password;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // DELETE - Delete user
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users
            .FindAsync(id);

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
}