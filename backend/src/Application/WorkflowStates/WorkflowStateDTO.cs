using LinearClone.Domain.Entities;

namespace LinearClone.Application.WorkflowStates;

public record WorkflowStateDto(
    Guid Id,
    Guid TeamId,
    string Name,
    int SortOrder,
    StateCategory Category);

