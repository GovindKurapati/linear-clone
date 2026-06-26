using LinearClone.Application.Common;
using LinearClone.Domain.Entities;
using LinearClone.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LinearClone.Infrastructure.Persistence;

// Inherits IdentityDbContext so ASP.NET Core Identity's tables (AspNetUsers, etc.)
// are created and managed alongside our own. ApplicationUser is our extended user.
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
    public DbSet<Issue> Issues => Set<Issue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MUST call base first — Identity configures its own entity tables here.
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Name).IsRequired().HasMaxLength(100);
            entity.Property(w => w.Slug).IsRequired().HasMaxLength(50);
            entity.HasIndex(w => w.Slug).IsUnique();
        });

        modelBuilder.Entity<Membership>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.UserId).IsRequired().HasMaxLength(450); // Identity key length
            entity.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(m => m.Workspace)
                  .WithMany(w => w.Memberships)
                  .HasForeignKey(m => m.WorkspaceId)
                  .OnDelete(DeleteBehavior.Cascade);

            // A user can belong to a workspace only once.
            entity.HasIndex(m => new { m.UserId, m.WorkspaceId }).IsUnique();
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Key).IsRequired().HasMaxLength(10);

            // Team key uniqueness is now PER WORKSPACE, not global.
            entity.HasIndex(t => new { t.WorkspaceId, t.Key }).IsUnique();

            entity.HasOne(t => t.Workspace)
                  .WithMany(w => w.Teams)
                  .HasForeignKey(t => t.WorkspaceId)
                  .OnDelete(DeleteBehavior.Cascade);
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

            // Board queries filter by team + state and order by Position — index supports that.
            entity.HasIndex(i => new { i.TeamId, i.StateId, i.Position });
        });

        // ---- Multi-tenant global query filters ----
        // Every query against these entities is automatically scoped to the current
        // user's workspace. Configured once here; applies everywhere. The filter reads
        // _currentUser.WorkspaceId LAZILY at query time (the lambda captures `this`,
        // not a snapshot), so it reflects the actual request's workspace.
        //
        // Team has WorkspaceId directly. WorkflowState and Issue reach the workspace
        // through their Team, so we filter on Team.WorkspaceId via the navigation.
        modelBuilder.Entity<Team>()
            .HasQueryFilter(t => _currentUser!.WorkspaceId == null || t.WorkspaceId == _currentUser.WorkspaceId);

        modelBuilder.Entity<WorkflowState>()
            .HasQueryFilter(s => _currentUser!.WorkspaceId == null || s.Team.WorkspaceId == _currentUser.WorkspaceId);

        modelBuilder.Entity<Issue>()
            .HasQueryFilter(i => _currentUser!.WorkspaceId == null || i.Team.WorkspaceId == _currentUser.WorkspaceId);
    }
}