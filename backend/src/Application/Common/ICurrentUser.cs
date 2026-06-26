namespace LinearClone.Application.Common;

// Abstraction over "who is making this request". Defined in Application so services
// and the DbContext filter can depend on it without referencing HttpContext.
// Implemented in the Api layer where HttpContext is available.
public interface ICurrentUser
{
    // The Identity user id from the token, or null if unauthenticated.
    string? UserId { get; }

    // The workspace the request is scoped to, or null if unauthenticated /
    // not yet resolved. All tenant data is filtered by this.
    Guid? WorkspaceId { get; }

    bool IsAuthenticated { get; }
}