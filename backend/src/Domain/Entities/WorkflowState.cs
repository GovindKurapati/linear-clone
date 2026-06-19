namespace LinearClone.Domain.Entities;

// The semantic bucket a state falls into, independent of its custom display name.
// Lets the app reason about "is this issue done?" even when a team renames
// "Done" to "Shipped to prod". Used later by cycle rollover (Phase 7).
public enum StateCategory
{
    Backlog = 0,
    Unstarted = 1,
    Started = 2,
    Completed = 3,
    Canceled = 4
}

public class WorkflowState
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }

    // Custom, user-facing name, e.g. "In Review"
    public string Name { get; set; } = string.Empty;

    // Left-to-right column order on the board
    public int SortOrder { get; set; }

    public StateCategory Category { get; set; }

    // Navigation
    public Team Team { get; set; } = null!;
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}