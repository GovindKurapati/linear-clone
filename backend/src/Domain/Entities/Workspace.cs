namespace LinearClone.Domain.Entities;

// The top-level tenant. Everything below it (teams, issues) belongs to exactly
// one workspace, and all data access is scoped to the caller's workspace.
public class Workspace
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // URL-friendly unique identifier, e.g. "acme" for acme.linear-clone.app
    public string Slug { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}