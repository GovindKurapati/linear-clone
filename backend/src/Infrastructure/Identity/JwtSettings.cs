namespace LinearClone.Infrastructure.Identity;

// Bound from the "Jwt" config section. The Key is a secret — store it in User
// Secrets locally, never in committed appsettings.
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}