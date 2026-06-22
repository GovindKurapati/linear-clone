using LinearClone.Application.WorkflowStates;
using LinearClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LinearClone.Infrastructure.WorkflowStates;

public class WorkflowStateService : IWorkflowStateService
{
    private readonly AppDbContext _db;

    public WorkflowStateService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<WorkflowStateDto>> GetByTeamAsync(Guid teamId, CancellationToken ct = default)
    {
        // Ordered by SortOrder so the frontend renders columns left-to-right correctly.
        return await _db.WorkflowStates
            .Where(s => s.TeamId == teamId)
            .OrderBy(s => s.SortOrder)
            .Select(s => new WorkflowStateDto(s.Id, s.TeamId, s.Name, s.SortOrder, s.Category))
            .ToListAsync(ct);
    }
}