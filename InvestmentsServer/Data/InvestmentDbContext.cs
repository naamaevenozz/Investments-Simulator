using Microsoft.EntityFrameworkCore;
using InvestmentsServer.Models;

namespace InvestmentsServer.Data;

/// <summary>
/// Database context for the Investment application.
/// Manages the connection and operations with the database.
/// </summary>
public class InvestmentDbContext : DbContext
{
    public InvestmentDbContext(DbContextOptions<InvestmentDbContext> options) 
        : base(options)
    {
    }

    /// <summary>
    /// User accounts table
    /// </summary>
    public DbSet<UserAccount> Users { get; set; } = null!;

    /// <summary>
    /// Active investments table
    /// </summary>
    public DbSet<ActiveInvestment> ActiveInvestments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure UserAccount entity
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(u => u.Username);
            
            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Balance)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // One-to-many relationship: User -> ActiveInvestments
            entity.HasMany(u => u.ActiveInvestments)
                .WithOne()
                .HasForeignKey(a => a.Username)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ActiveInvestment entity
        modelBuilder.Entity<ActiveInvestment>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(a => a.ExpectedReturn)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(a => a.EndTime)
                .IsRequired();

            // Index for faster queries
            entity.HasIndex(a => a.EndTime);
            entity.HasIndex(a => a.Username);
        });
    }
}