using LinearClone.Application.Auth;
using LinearClone.Application.Common;
using LinearClone.Domain.Entities;
using LinearClone.Infrastructure.Identity;
using LinearClone.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinearClone.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // Registration creates three things together: the user, their workspace,
        // and an Owner membership linking them. All-or-nothing via a transaction.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        // UserManager handles password hashing and validation rules.
        var created = await _userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            var errors = string.Join(" ", created.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create account: {errors}");
        }

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = request.WorkspaceName,
            Slug = await GenerateUniqueSlugAsync(request.WorkspaceName, ct),
            CreatedAt = DateTime.UtcNow,
        };
        _db.Workspaces.Add(workspace);

        _db.Memberships.Add(new Membership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            WorkspaceId = workspace.Id,
            Role = MembershipRole.Owner,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new AuthResponse(
            _tokenService.CreateToken(user, workspace.Id),
            user.Id, user.Email!, user.DisplayName ?? "",
            workspace.Id, workspace.Name);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        // Same generic error whether the email is unknown or the password is wrong —
        // don't leak which accounts exist.
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            throw new InvalidOperationException("Invalid email or password.");

        // Resolve the user's workspace (they have at least one from registration).
        var membership = await _db.Memberships
            .Include(m => m.Workspace)
            .Where(m => m.UserId == user.Id)
            .OrderBy(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("This account has no workspace.");

        return new AuthResponse(
            _tokenService.CreateToken(user, membership.WorkspaceId),
            user.Id, user.Email!, user.DisplayName ?? "",
            membership.WorkspaceId, membership.Workspace.Name);
    }

    // Turns "Acme Corp" into "acme-corp", appending a number if taken.
    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken ct)
    {
        var baseSlug = new string(name.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Trim('-');
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "workspace";

        var slug = baseSlug;
        var suffix = 1;
        while (await _db.Workspaces.AnyAsync(w => w.Slug == slug, ct))
            slug = $"{baseSlug}-{++suffix}";

        return slug;
    }
}
