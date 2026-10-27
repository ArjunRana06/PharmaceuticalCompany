using Microsoft.EntityFrameworkCore;
using Project.Models;

namespace Project.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<Drug> Drugs { get; set; } = null!;
    public DbSet<DrugRecall> DrugRecalls { get; set; } = null!;
    public DbSet<DdaNotice> DdaNotices { get; set; } = null!;
    public DbSet<DrugCategory> DrugCategories { get; set; } = null!;
}