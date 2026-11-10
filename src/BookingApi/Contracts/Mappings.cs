using BookingApi.Domain;

namespace BookingApi.Contracts;

public static class Mappings
{
    public static ServiceResponse ToResponse(this SalonService s) =>
        new(s.Id, s.Name, s.Description, s.DurationMinutes, s.Price, s.IsActive);

    public static MasterResponse ToResponse(this Master m) =>
        new(
            m.Id,
            m.Name,
            m.Specialization,
            m.PhotoUrl,
            m.IsActive,
            m.Services.Select(s => s.Id).Order().ToList(),
            m.WorkingHours
                .OrderBy(h => h.DayOfWeek)
                .Select(h => new WorkingHoursDto(h.DayOfWeek, h.StartTime, h.EndTime))
                .ToList());

    /// <summary>Requires <see cref="Booking.Master"/> and <see cref="Booking.Service"/> to be loaded.</summary>
    public static BookingResponse ToResponse(this Booking b) =>
        new(
            b.Id,
            b.MasterId,
            b.Master.Name,
            b.ServiceId,
            b.Service.Name,
            b.Service.Price,
            b.Date,
            b.StartTime,
            b.EndTime,
            b.ClientName,
            b.ClientPhone,
            b.Status,
            b.CreatedAtUtc);
}
