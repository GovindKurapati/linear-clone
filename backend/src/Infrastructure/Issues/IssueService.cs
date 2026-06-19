using LinearClone.Application.Common;
using LinearClone.Application.Issues;
using LinearClone.Domain.Entities;
using LinearClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LinearClone.Infrastructure.Issues;

public class IssueService : IIssueService
{
    private readonly AppDbContext _db;

    public IssueService(AppDbContext db) => _db = db;

    public async Task<IssueDto> CreateAsync(CreateIssueRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.", nameof(request));

        // Wrap the whole create in a transaction. The risk we're guarding against:
        // two issues created for the same team at the same time both reading
        // NextIssueNumber = 42 and both becoming ENG-42. The transaction plus the
        // unique index on (TeamId, Number) ensures that can't silently happen.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var team = await _db.Teams
            .FirstOrDefaultAsync(t => t.Id == request.TeamId, ct)
            ?? throw new NotFoundException($"Team {request.TeamId} not found.");

        // Resolve the target state: explicit, or the team's default (lowest SortOrder).
        var stateId = request.StateId
            ?? await _db.WorkflowStates
                .Where(s => s.TeamId == request.TeamId)
                .OrderBy(s => s.SortOrder)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);

        if (stateId == Guid.Empty)
            throw new InvalidOperationException("Team has no workflow states to place the issue in.");

        // Validate parent belongs to the same team, if provided.
        if (request.ParentId is { } parentId)
        {
            var parentExists = await _db.Issues
                .AnyAsync(i => i.Id == parentId && i.TeamId == request.TeamId, ct);
            if (!parentExists)
                throw new InvalidOperationException("Parent issue not found in this team.");
        }

        // Grab and increment the team's running number.
        var number = team.NextIssueNumber;
        team.NextIssueNumber += 1;

        // Generate the sort key: append to the end of the target column.
        var lastSortKey = await _db.Issues
            .Where(i => i.TeamId == request.TeamId && i.StateId == stateId)
            .OrderByDescending(i => i.SortKey)
            .Select(i => i.SortKey)
            .FirstOrDefaultAsync(ct);

        var now = DateTime.UtcNow;
        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            TeamId = request.TeamId,
            StateId = stateId,
            ParentId = request.ParentId,
            Number = number,
            Title = request.Title.Trim(),
            Description = request.Description,
            Priority = request.Priority,
            Estimate = request.Estimate,
            SortKey = FractionalIndex.KeyAfter(lastSortKey),
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Issues.Add(issue);
        await _db.SaveChangesAsync(ct);   // persists both the new issue and the team's incremented counter
        await tx.CommitAsync(ct);

        return ToDto(issue, team.Key);
    }

    public async Task<IssueDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var issue = await _db.Issues
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        return issue is null ? null : ToDto(issue, issue.Team.Key);
    }

    public async Task<IReadOnlyList<IssueListItemDto>> GetByTeamAsync(Guid teamId, CancellationToken ct = default)
    {
        // Project straight to the list DTO inside the query, so SQL only selects the
        // columns a card needs — not the full row. Exclude archived (soft-deleted) issues.
        // The team Key is pulled via the navigation so we can compose the identifier.
        var items = await _db.Issues
            .Where(i => i.TeamId == teamId && !i.IsArchived)
            .OrderBy(i => i.StateId).ThenBy(i => i.SortKey)
            .Select(i => new IssueListItemDto(
                i.Id,
                i.StateId,
                i.Number,
                i.Team.Key + "-" + i.Number,
                i.Title,
                i.Priority,
                i.Estimate,
                i.SortKey))
            .ToListAsync(ct);

        return items;
    }

    public async Task<IssueDto> UpdateAsync(Guid id, UpdateIssueRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.", nameof(request));

        var issue = await _db.Issues
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"Issue {id} not found.");

        // Validate the target state belongs to the same team (can't move an issue
        // into another team's column).
        var stateOk = await _db.WorkflowStates
            .AnyAsync(s => s.Id == request.StateId && s.TeamId == issue.TeamId, ct);
        if (!stateOk)
            throw new InvalidOperationException("Target state does not belong to this issue's team.");

        // This is the optimistic-concurrency setup. Tell EF the value of the row's
        // concurrency token AS THE CLIENT LAST SAW IT. When SaveChanges runs, EF builds
        // an UPDATE ... WHERE Id = @id AND RowVersion = @originalRowVersion. If another
        // edit changed the row in the meantime, RowVersion no longer matches, zero rows
        // update, and EF throws DbUpdateConcurrencyException.
        _db.Entry(issue).Property(i => i.RowVersion).OriginalValue = request.RowVersion;

        issue.Title = request.Title.Trim();
        issue.Description = request.Description;
        issue.Priority = request.Priority;
        issue.Estimate = request.Estimate;
        issue.StateId = request.StateId;
        issue.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Translate EF's exception into our own layer-neutral one. The controller
            // turns this into 409 Conflict, telling the client to reload and retry.
            throw new ConcurrencyConflictException(
                "This issue was modified by someone else. Reload and try again.");
        }

        return ToDto(issue, issue.Team.Key);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"Issue {id} not found.");

        // Soft delete: flag it rather than removing the row, preserving history
        // and keeping the audit trail intact (consistent with the schema design).
        issue.IsArchived = true;
        issue.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static IssueDto ToDto(Issue i, string teamKey) => new(
        i.Id,
        i.TeamId,
        i.StateId,
        i.ParentId,
        i.Number,
        $"{teamKey}-{i.Number}",   // compose the ENG-123 identifier here
        i.Title,
        i.Description,
        i.Priority,
        i.Estimate,
        i.SortKey,
        i.IsArchived,
        i.CreatedAt,
        i.UpdatedAt,
        i.RowVersion);
}