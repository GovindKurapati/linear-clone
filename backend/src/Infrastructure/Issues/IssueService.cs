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
        // Position the new issue at the end of its target column: just past the
        // current max Position in that state (or the default Gap if the column is empty).
        var maxPosition = await _db.Issues
            .Where(i => i.TeamId == request.TeamId && i.StateId == stateId)
            .Select(i => (double?)i.Position)
            .MaxAsync(ct);

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
            Position = PositionHelper.Append(maxPosition),
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
            .OrderBy(i => i.StateId).ThenBy(i => i.Position)
            .Select(i => new IssueListItemDto(
                i.Id,
                i.StateId,
                i.Number,
                i.Team.Key + "-" + i.Number,
                i.Title,
                i.Priority,
                i.Estimate,
                i.Position,
                i.RowVersion))
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

    public async Task<IssueDto> ReorderAsync(Guid id, ReorderIssueRequest request, CancellationToken ct = default)
    {
        var issue = await _db.Issues
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"Issue {id} not found.");

        // Validate the target state belongs to the issue's team.
        var stateOk = await _db.WorkflowStates
            .AnyAsync(s => s.Id == request.TargetStateId && s.TeamId == issue.TeamId, ct);
        if (!stateOk)
            throw new InvalidOperationException("Target state does not belong to this issue's team.");

        // Look up the positions of the two neighbors the client dropped between.
        // Either may be null at a column edge (top or bottom).
        double? beforePos = request.BeforeIssueId is { } beforeId
            ? await _db.Issues.Where(i => i.Id == beforeId).Select(i => (double?)i.Position).FirstOrDefaultAsync(ct)
            : null;

        double? afterPos = request.AfterIssueId is { } afterId
            ? await _db.Issues.Where(i => i.Id == afterId).Select(i => (double?)i.Position).FirstOrDefaultAsync(ct)
            : null;

        // If the two neighbors are too close to subdivide, rebalance the target
        // column first (reassign clean, evenly-spaced positions), then recompute.
        if (beforePos is { } bp && afterPos is { } ap && PositionHelper.NeedsRebalance(bp, ap))
        {
            await RebalanceColumnAsync(issue.TeamId, request.TargetStateId, ct);
            beforePos = request.BeforeIssueId is { } bId
                ? await _db.Issues.Where(i => i.Id == bId).Select(i => (double?)i.Position).FirstOrDefaultAsync(ct)
                : null;
            afterPos = request.AfterIssueId is { } aId
                ? await _db.Issues.Where(i => i.Id == aId).Select(i => (double?)i.Position).FirstOrDefaultAsync(ct)
                : null;
        }

        issue.StateId = request.TargetStateId;
        issue.Position = PositionHelper.Between(beforePos, afterPos);
        issue.UpdatedAt = DateTime.UtcNow;

        // Same optimistic-concurrency guard as UpdateAsync.
        _db.Entry(issue).Property(i => i.RowVersion).OriginalValue = request.RowVersion;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "This issue was modified by someone else. Reload and try again.");
        }

        return ToDto(issue, issue.Team.Key);
    }

    // Reassigns clean, evenly-spaced positions to every issue in a column, in their
    // current order. The rare fallback when a gap between two neighbors is exhausted.
    private async Task RebalanceColumnAsync(Guid teamId, Guid stateId, CancellationToken ct)
    {
        var issues = await _db.Issues
            .Where(i => i.TeamId == teamId && i.StateId == stateId && !i.IsArchived)
            .OrderBy(i => i.Position)
            .ToListAsync(ct);

        var pos = PositionHelper.Gap;
        foreach (var i in issues)
        {
            i.Position = pos;
            pos += PositionHelper.Gap;
        }
        await _db.SaveChangesAsync(ct);
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
        i.Position,
        i.IsArchived,
        i.CreatedAt,
        i.UpdatedAt,
        i.RowVersion);
}