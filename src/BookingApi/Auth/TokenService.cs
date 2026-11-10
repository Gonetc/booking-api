using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BookingApi.Auth;

public record AccessToken(string Token, DateTime ExpiresAtUtc);

public class TokenService(IOptions<JwtOptions> options)
{
    private static readonly JsonWebTokenHandler Handler = new();

    public AccessToken CreateAdminToken(string username)
    {
        var jwt = options.Value;

        // Token lifetime is checked by JwtBearer against the real clock, so the real clock is used here too
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(jwt.ExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(
            [
                new Claim(TokenClaims.Subject, username),
                new Claim(TokenClaims.Role, Roles.Admin)
            ]),
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(Handler.CreateToken(descriptor), expires);
    }
}
