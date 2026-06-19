namespace LinearClone.Application.Issues;

public interface IIssueService
{
    Task<IssueDto> CreateAsync(CreateIssueRequest request, CancellationToken ct = default);
    Task<IssueDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<IssueListItemDto>> GetByTeamAsync(Guid teamId, CancellationToken ct = default);
    Task<IssueDto> UpdateAsync(Guid id, UpdateIssueRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}