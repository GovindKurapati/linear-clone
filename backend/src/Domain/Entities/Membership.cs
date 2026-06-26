namespace LinearClone.Domain.Entities;

public enum MembershipRole
{
    // Can read and write issues, but not manage the workspace itself.
    Member = 0,
    // Full control: manage teams, invite/remove members, delete the workspace.
    Admin = 1,
    // The user who created the workspace. Like Admin, plus billing/ownership.
    Owner = 2
}

// Join entity: which user belongs to which workspace, and in what role.
// References the Identity user by string id (UserId) rather than a navigation,
// so the Domain stays free of any Identity/EF dependency.
public class Membership
{
    public Guid Id { get; set; }

    // The ASP.NET Core Identity user id (string key).
    public string UserId { get; set; } = string.Empty;

    public Guid WorkspaceId { get; set; }

    public MembershipRole Role { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation (workspace only — user lives in the Identity store)
    public Workspace Workspace { get; set; } = null!;
}