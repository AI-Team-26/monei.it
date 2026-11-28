using Microsoft.EntityFrameworkCore;
using Monei.Core.Entities;

namespace Monei.Core.Data;

// Database-first: the model mirrors the existing SQL Server tables.
// No relationships are declared because the database defines no foreign keys.
public class MoneiDbContext : DbContext
{
    public MoneiDbContext(string connectionString)
        : base(new DbContextOptionsBuilder<MoneiDbContext>().UseSqlServer(connectionString).Options)
    {
    }

    public MoneiDbContext(DbContextOptions<MoneiDbContext> options) : base(options)
    {
    }

    public DbSet<RegistryEntry> RegistryEntries => Set<RegistryEntry>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Subcategory> Subcategories => Set<Subcategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistryEntry>(e =>
        {
            e.ToTable("Registry");
            e.HasKey(x => x.Id);
            e.Property(x => x.OperationType).HasColumnType("smallint");
            e.Property(x => x.Amount).HasColumnType("decimal(12,5)");
            e.Property(x => x.CreationDate).HasColumnType("smalldatetime");
            e.Property(x => x.LastChangeDate).HasColumnType("smalldatetime");
            e.Property(x => x.Date).HasColumnType("date");
            e.Property(x => x.Note).HasColumnType("nvarchar(1000)");
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("Account");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasColumnType("nvarchar(50)");
            e.Property(x => x.Password).HasColumnType("nvarchar(128)");
            e.Property(x => x.Role).HasColumnType("nvarchar(30)");
            e.Property(x => x.CreationDate).HasColumnType("smalldatetime");
            e.Property(x => x.LastChangeDate).HasColumnType("smalldatetime");
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Category");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasColumnType("nvarchar(50)");
            e.Property(x => x.Description).HasColumnType("nvarchar(1000)");
            e.Property(x => x.ImageName).HasColumnType("varchar(50)");
            e.Property(x => x.CreationDate).HasColumnType("smalldatetime");
            e.Property(x => x.LastChangeDate).HasColumnType("smalldatetime");
            e.Property(x => x.DeletionDate).HasColumnType("date");
        });

        modelBuilder.Entity<Subcategory>(e =>
        {
            e.ToTable("Subcategory");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CategoryId, x.Name }).IsUnique();
            e.Property(x => x.Name).HasColumnType("nvarchar(100)");
            e.Property(x => x.Description).HasColumnType("nvarchar(1000)");
            e.Property(x => x.CreationDate).HasColumnType("smalldatetime");
            e.Property(x => x.LastChangeDate).HasColumnType("smalldatetime");
            e.Property(x => x.DeletionDate).HasColumnType("date");
        });
    }
}
