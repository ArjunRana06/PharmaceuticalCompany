using Microsoft.EntityFrameworkCore;
using Project.Models;
using Project.Services;

namespace Project.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (configuration.GetValue("Database:AutoMigrate", false))
        {
            try
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Automatic database migration failed.");
            }
        }

        var email = configuration["AdminSeed:Email"]?.Trim();
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation(
                "Admin seeding skipped. Set AdminSeed__Email and AdminSeed__Password to create the first admin.");
            return;
        }

        if (password.Length < 8)
        {
            logger.LogWarning("Admin seeding skipped: AdminSeed:Password must be at least 8 characters.");
            return;
        }

        try
        {
            if (await context.Users.AnyAsync(u => u.Role == "Admin"))
            {
                logger.LogInformation("Admin seeding skipped: an admin user already exists.");
                return;
            }

            var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();
            var name = configuration["AdminSeed:Name"]?.Trim();
            var admin = new User
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Administrator" : name,
                Email = email,
                Role = "Admin"
            };

            admin.Password = passwordService.Hash(admin, password);
            context.Users.Add(admin);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded admin user {Email}.", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Admin seeding failed.");
        }
    }
}
