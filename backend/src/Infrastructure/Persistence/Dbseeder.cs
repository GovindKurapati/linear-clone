using LinearClone.Application.Common;
using LinearClone.Domain.Entities;
using LinearClone.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinearClone.Infrastructure.Persistence;

public static class DbSeeder
{
    // Demo credentials you can log in with after seeding.
    public const string DemoEmail = "demo@example.com";
    public const string DemoPassword = "Password123";

    // Idempotent: only seeds if no workspace exists yet. Now needs UserManager
    // so it can create a real (hashed-password) Identity user to log in as.
    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        CancellationToken ct = default)
    {
        if (await db.Workspaces.AnyAsync(ct))
            return;

        // 1. Workspace (the tenant) — created first; everything references it.
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Demo Workspace",
            Slug = "demo",
            CreatedAt = DateTime.UtcNow,
        };
        db.Workspaces.Add(workspace);

        // 2. Demo user via UserManager (handles password hashing).
        var user = new ApplicationUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            DisplayName = "Demo User",
        };
        var created = await userManager.CreateAsync(user, DemoPassword);
        if (!created.Succeeded)
        {
            var errors = string.Join(" ", created.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Seed user creation failed: {errors}");
        }

        // 3. Membership: the demo user owns the demo workspace.
        db.Memberships.Add(new Membership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            WorkspaceId = workspace.Id,
            Role = MembershipRole.Owner,
            CreatedAt = DateTime.UtcNow,
        });

        // 4. Team under the workspace.
        var team = new Team
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            Name = "Engineering",
            Key = "ENG",
            NextIssueNumber = 1,
            CreatedAt = DateTime.UtcNow,
        };
        db.Teams.Add(team);

        // 5. Workflow states for the team.
        var states = new List<WorkflowState>
        {
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "Backlog",     SortOrder = 0, Category = StateCategory.Backlog },
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "Todo",        SortOrder = 1, Category = StateCategory.Unstarted },
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "In Progress", SortOrder = 2, Category = StateCategory.Started },
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "In Review",   SortOrder = 3, Category = StateCategory.Started },
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "Done",        SortOrder = 4, Category = StateCategory.Completed },
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "Canceled",    SortOrder = 5, Category = StateCategory.Canceled },
        };
        db.WorkflowStates.AddRange(states);

        // 6. A couple of starter issues in the Backlog, with spaced positions.
        var backlog = states[0];
        db.Issues.AddRange(
            new Issue
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                StateId = backlog.Id,
                Number = team.NextIssueNumber++,
                Title = "Set up the project",
                Priority = IssuePriority.High,
                Position = PositionHelper.Gap,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Issue
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                StateId = backlog.Id,
                Number = team.NextIssueNumber++,
                Title = "Design the board UI",
                Priority = IssuePriority.Medium,
                Position = PositionHelper.Gap * 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

        await db.SaveChangesAsync(ct);
    }
}