using System.ComponentModel.DataAnnotations;

namespace BookingApi.Contracts;

public record ServiceResponse(
    int Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    bool IsActive);

public record UpsertServiceRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(1000)] string? Description,
    [Range(5, 480)] int DurationMinutes,
    [Range(0.0, 1_000_000.0)] decimal Price,
    bool IsActive = true);
