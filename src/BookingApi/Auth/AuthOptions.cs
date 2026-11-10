using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BookingApi.Auth;

/// <summary>Settings for issuing and validating JWT access tokens.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "BookingApi";

    [Required]
    public string Audience { get; set; } = "BookingClients";

    /// <summary>HMAC-SHA256 secret, at least 32 bytes. Never commit a production key.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = "";

    [Range(1, 24 * 60)]
    public int ExpirationMinutes { get; set; } = 60;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}

/// <summary>
/// Credentials of the salon administrator. A single admin account is enough for this product;
/// in production pass them via environment variables or a secret store.
/// </summary>
public class AdminOptions
{
    public const string SectionName = "Admin";

    [Required]
    public string Username { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";
}

public static class Roles
{
    public const string Admin = "Admin";
}

/// <summary>Claim names written into the token (kept short, no WS-* URIs).</summary>
public static class TokenClaims
{
    public const string Subject = "sub";
    public const string Role = "role";
}

public static class RateLimitPolicies
{
    public const string Login = "login";
}
