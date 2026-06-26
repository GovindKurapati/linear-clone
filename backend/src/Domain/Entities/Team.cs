namespace LinearClone.Domain.Entities;

public class Team
{
    public Guid Id { get; set; }

    // The workspace (tenant) this team belongs to.
    public Guid WorkspaceId { get; set; }

    // Display name, e.g. "Engineering"
    public string Name { get; set; } = string.Empty;

    // Short prefix used in issue identifiers, e.g. "ENG" -> ENG-123
    public string Key { get; set; } = string.Empty;

    // The next issue number to hand out for this team. Assign this value to a new
    // issue, then increment. Seeded at 1. Kept on the team so numbering is per-team.
    public int NextIssueNumber { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public ICollection<WorkflowState> WorkflowStates { get; set; } = new List<WorkflowState>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}