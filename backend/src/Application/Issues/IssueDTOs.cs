using LinearClone.Domain.Entities;

namespace LinearClone.Application.Issues;

// Incoming payload from the client. No Id/Number/SortKey — the server owns those.
// StateId is optional: if omitted, the service drops the issue into the team's
// default (lowest SortOrder) state.
public record CreateIssueRequest(
    Guid TeamId,
    string Title,
    string? Description,
    IssuePriority Priority,
    int? Estimate,
    Guid? StateId,
    Guid? ParentId);

// What we return to the client. Includes the composed identifier (ENG-123).
public record IssueDto(
    Guid Id,
    Guid TeamId,
    Guid StateId,
    Guid? ParentId,
    int Number,
    string Identifier,   // e.g. "ENG-123" — composed, never stored
    string Title,
    string? Description,
    IssuePriority Priority,
    int? Estimate,
    double Position,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    byte[] RowVersion);  // round-trips to the client for optimistic concurrency

// Update payload. RowVersion is the token the client last saw — the server compares
// it against the current DB value and rejects the save if they differ (someone else
// edited the row first). All editable fields are sent; StateId moves the issue's column.
public record UpdateIssueRequest(
    string Title,
    string? Description,
    IssuePriority Priority,
    int? Estimate,
    Guid StateId,
    byte[] RowVersion);

// Lightweight projection for list/board views. Includes RowVersion because the
// board needs the concurrency token to issue reorder calls without a full re-fetch.
public record IssueListItemDto(
    Guid Id,
    Guid StateId,
    int Number,
    string Identifier,
    string Title,
    IssuePriority Priority,
    int? Estimate,
    double Position,
    byte[] RowVersion);

// Reorder/move payload from the board. The client sends the target state and the
// ids of the issues that will sit immediately above and below the dropped issue
// (either may be null at a column edge). The server computes the new Position by
// averaging the neighbors. RowVersion guards against concurrent edits.
public record ReorderIssueRequest(
    Guid TargetStateId,
    Guid? BeforeIssueId,   // the issue directly above the drop point (null = top)
    Guid? AfterIssueId,    // the issue directly below the drop point (null = bottom)
    byte[] RowVersion);