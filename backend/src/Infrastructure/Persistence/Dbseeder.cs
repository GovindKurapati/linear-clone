using LinearClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinearClone.Infrastructure.Persistence;

public static class DbSeeder
{
    // Idempotent: safe to run on every startup. Only seeds if the DB is empty.
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // If any team already exists, assume seeding has run — do nothing.
        if (await db.Teams.AnyAsync(ct))
            return;

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Engineering",
            Key = "ENG",
            NextIssueNumber = 1,
            CreatedAt = DateTime.UtcNow
        };

        // A standard workflow, ordered left-to-right, each mapped to a semantic category.
        var states = new List<WorkflowState>
        {
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "Backlog",     SortOrder = 0, Category = StateCategory.Backlog },
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "Todo",        SortOrder = 1, Category = StateCategory.Unstarted },
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "In Progress", SortOrder = 2, Category = StateCategory.Started },
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "In Review",   SortOrder = 3, Category = StateCategory.Started },
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "Done",        SortOrder = 4, Category = StateCategory.Completed },
            new() { Id = Guid.NewGuid(), TeamId = team.Id, Name = "Canceled",    SortOrder = 5, Category = StateCategory.Canceled },
        };

        db.Teams.Add(team);
        db.WorkflowStates.AddRange(states);
        await db.SaveChangesAsync(ct);
    }
}