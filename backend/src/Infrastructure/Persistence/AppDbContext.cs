using LinearClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinearClone.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
    public DbSet<Issue> Issues => Set<Issue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Key).IsRequired().HasMaxLength(10);

            // Team keys must be unique so ENG-123 is unambiguous.
            entity.HasIndex(t => t.Key).IsUnique();
        });

        modelBuilder.Entity<WorkflowState>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name).IsRequired().HasMaxLength(50);

            // Store the enum as a readable string in the DB rather than an int,
            // so raw SQL / reports are legible. Costs a little space, worth it here.
            entity.Property(s => s.Category)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // Team -> WorkflowState (one-to-many).
            // Cascade: deleting a team removes its states (states can't outlive their team).
            entity.HasOne(s => s.Team)
                  .WithMany(t => t.WorkflowStates)
                  .HasForeignKey(s => s.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.TeamId, s.SortOrder });
        });

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Title).IsRequired().HasMaxLength(255);
            entity.Property(i => i.Priority).HasConversion<string>().HasMaxLength(20);
            entity.Property(i => i.SortKey).IsRequired().HasMaxLength(50);

            // SQL Server rowversion column for optimistic concurrency.
            // IsRowVersion() maps it to the rowversion type and tells EF to use it as
            // the concurrency token automatically on every UPDATE/DELETE.
            entity.Property(i => i.RowVersion).IsRowVersion();

            // Team -> Issue (one-to-many).
            // Restrict: you can't delete a team that still has issues — forces a
            // deliberate cleanup rather than silently nuking work.
            entity.HasOne(i => i.Team)
                  .WithMany(t => t.Issues)
                  .HasForeignKey(i => i.TeamId)
                  .OnDelete(DeleteBehavior.Restrict);

            // WorkflowState -> Issue (one-to-many).
            // Restrict: can't delete a column that still holds issues; move them first.
            entity.HasOne(i => i.State)
                  .WithMany(s => s.Issues)
                  .HasForeignKey(i => i.StateId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Issue -> Issue self-reference (parent / sub-issues).
            // Restrict: deleting a parent that still has children is blocked;
            // also avoids SQL Server's "multiple cascade paths" error.
            entity.HasOne(i => i.Parent)
                  .WithMany(i => i.SubIssues)
                  .HasForeignKey(i => i.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            // The human-facing identifier (TeamId + Number) must be unique per team.
            entity.HasIndex(i => new { i.TeamId, i.Number }).IsUnique();

            // Board queries filter by team + state and order by SortKey — index supports that.
            entity.HasIndex(i => new { i.TeamId, i.StateId, i.SortKey });
        });
    }
}