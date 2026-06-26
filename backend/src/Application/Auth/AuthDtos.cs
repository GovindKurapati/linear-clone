using System.ComponentModel.DataAnnotations;

namespace LinearClone.Application.Auth;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string DisplayName,
    [Required] string WorkspaceName);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

// Returned on successful register/login. The token goes in the Authorization
// header on subsequent requests; the rest is convenience for the client UI.
public record AuthResponse(
    string Token,
    string UserId,
    string Email,
    string DisplayName,
    Guid WorkspaceId,
    string WorkspaceName);