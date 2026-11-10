using System.ComponentModel.DataAnnotations;

namespace BookingApi.Contracts;

public record LoginRequest(
    [Required, StringLength(100)] string Username,
    [Required, StringLength(200)] string Password);

public record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc);
