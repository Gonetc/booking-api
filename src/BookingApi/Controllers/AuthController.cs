using System.Security.Cryptography;
using System.Text;
using BookingApi.Auth;
using BookingApi.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace BookingApi.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IOptions<AdminOptions> adminOptions, TokenService tokens) : ControllerBase
{
    /// <summary>Exchanges admin credentials for a JWT access token. Limited to 5 attempts per minute per IP.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var admin = adminOptions.Value;

        // Non-short-circuit '&' so both comparisons always run
        var valid = SecureEquals(request.Username, admin.Username) & SecureEquals(request.Password, admin.Password);
        if (!valid)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid username or password.");
        }

        var token = tokens.CreateAdminToken(admin.Username);
        return new LoginResponse(token.Token, "Bearer", token.ExpiresAtUtc);
    }

    /// <summary>Constant-time comparison to avoid leaking information through response timing.</summary>
    private static bool SecureEquals(string actual, string expected) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(actual), Encoding.UTF8.GetBytes(expected));
}
