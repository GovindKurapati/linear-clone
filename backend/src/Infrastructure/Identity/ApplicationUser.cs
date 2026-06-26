using Microsoft.AspNetCore.Identity;

namespace LinearClone.Infrastructure.Identity;

// Extends the built-in Identity user with our own fields. Lives in Infrastructure
// because IdentityUser is an EF/Identity type — keeping it out of the Domain.
public class ApplicationUser : IdentityUser
{
    // IdentityUser already provides Id (string), UserName, Email, PasswordHash, etc.
    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}