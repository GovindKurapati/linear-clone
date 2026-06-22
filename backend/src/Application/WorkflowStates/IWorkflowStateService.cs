namespace LinearClone.Application.WorkflowStates;

public interface IWorkflowStateService
{
    Task<IReadOnlyList<WorkflowStateDto>> GetByTeamAsync(Guid teamId, CancellationToken ct = default);
}