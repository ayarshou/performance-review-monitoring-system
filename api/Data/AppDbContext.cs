using Microsoft.EntityFrameworkCore;
using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ReviewSession> ReviewSessions => Set<ReviewSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Employee ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Position)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.HireDate)
                  .IsRequired();

            // Self-referencing one-to-many: one Manager (Employee) → many Subordinates (Employees)
            entity.HasOne(e => e.Manager)
                  .WithMany(e => e.Subordinates)
                  .HasForeignKey(e => e.ManagerId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

        // ── ReviewSession ────────────────────────────────────────────────────────
        modelBuilder.Entity<ReviewSession>(entity =>
        {
            entity.HasKey(rs => rs.Id);

            // Store the enum as a string for readability in the database
            entity.Property(rs => rs.Status)
                  .HasConversion<string>()
                  .IsRequired();

            entity.Property(rs => rs.ScheduledDate)
                  .IsRequired();

            entity.Property(rs => rs.Deadline)
                  .IsRequired();

            entity.HasOne(rs => rs.Employee)
                  .WithMany(e => e.ReviewSessions)
                  .HasForeignKey(rs => rs.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
