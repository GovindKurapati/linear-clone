namespace LinearClone.Domain.Entities;

public enum IssuePriority
{
    NoPriority = 0,
    Urgent = 1,
    High = 2,
    Medium = 3,
    Low = 4
}

public class Issue
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    public Guid StateId { get; set; }

    // Self-reference for sub-issues. Null = top-level issue.
    public Guid? ParentId { get; set; }

    // Per-team sequential number. Combined with Team.Key for display: ENG-123.
    // Stored as an int; the formatted string is composed, never persisted.
    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IssuePriority Priority { get; set; } = IssuePriority.NoPriority;

    // Story points / estimate. Nullable because not every issue is estimated.
    public int? Estimate { get; set; }

    // Fractional-index key for board ordering (Phase 2). String holds a LexoRank-style key.
    public string SortKey { get; set; } = string.Empty;

    // Soft delete. Archived issues stay in the DB for history/recovery.
    public bool IsArchived { get; set; }

    // SQL Server rowversion: optimistic concurrency token. Auto-managed by the DB —
    // changes on every update. EF compares it on save; a mismatch means someone else
    // edited the row first, throwing DbUpdateConcurrencyException. Never set manually.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Team Team { get; set; } = null!;
    public WorkflowState State { get; set; } = null!;
    public Issue? Parent { get; set; }
    public ICollection<Issue> SubIssues { get; set; } = new List<Issue>();
}