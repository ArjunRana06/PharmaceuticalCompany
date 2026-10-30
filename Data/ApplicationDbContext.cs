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
    public DbSet<RecallSyncLog> RecallSyncLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DrugRecall>()
            .HasIndex(r => new { r.SourceName, r.ExternalId })
            .IsUnique()
            .HasFilter("[ExternalId] IS NOT NULL");

        modelBuilder.Entity<DrugRecall>()
            .HasIndex(r => new { r.IsApproved, r.ApprovalStatus, r.RecallDate });
    }
}