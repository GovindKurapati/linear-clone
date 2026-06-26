using System.Security.Claims;
using LinearClone.Application.Common;
using Microsoft.AspNetCore.Http;

namespace LinearClone.Api.Infrastructure;

// Reads the authenticated user and selected workspace from the request token.
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? UserId =>
        _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated =>
        _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? WorkspaceId =>
        Guid.TryParse(
            _accessor.HttpContext?.User?.FindFirstValue("workspace_id"),
            out var workspaceId)
            ? workspaceId
            : null;
}
