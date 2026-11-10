using System.ComponentModel.DataAnnotations;

namespace BookingApi.Contracts;

public record WorkingHoursDto(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public record MasterResponse(
    int Id,
    string Name,
    string? Specialization,
    string? PhotoUrl,
    bool IsActive,
    IReadOnlyList<int> ServiceIds,
    IReadOnlyList<WorkingHoursDto> WorkingHours);

public record UpsertMasterRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(100)] string? Specialization,
    [Url, StringLength(500)] string? PhotoUrl,
    [Required] IReadOnlyList<int> ServiceIds,
    [Required] IReadOnlyList<WorkingHoursDto> WorkingHours,
    bool IsActive = true);

public record AvailabilityResponse(
    int MasterId,
    int ServiceId,
    DateOnly Date,
    IReadOnlyList<TimeOnly> Slots);
