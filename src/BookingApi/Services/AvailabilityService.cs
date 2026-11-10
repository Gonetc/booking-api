using BookingApi.Data;
using BookingApi.Domain;
using BookingApi.Errors;
using BookingApi.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookingApi.Services;

public class AvailabilityService(AppDbContext db, IOptions<SalonOptions> options, TimeProvider timeProvider)
{
    private readonly SalonOptions _options = options.Value;

    public async Task<IReadOnlyList<TimeOnly>> GetFreeSlotsAsync(
        int masterId, int serviceId, DateOnly date, CancellationToken ct)
    {
        var master = await db.Masters
            .AsNoTracking()
            .Include(m => m.WorkingHours)
            .Include(m => m.Services)
            .FirstOrDefaultAsync(m => m.Id == masterId && m.IsActive, ct)
            ?? throw new NotFoundException($"Master {masterId} was not found.");

        var service = master.Services.FirstOrDefault(s => s.Id == serviceId && s.IsActive)
            ?? throw new BusinessRuleException($"Master {masterId} does not provide service {serviceId}.");

        var (today, now) = GetSalonNow();
        if (date < today)
        {
            return [];
        }

        var hours = master.WorkingHours.FirstOrDefault(h => h.DayOfWeek == date.DayOfWeek);
        if (hours is null)
        {
            return [];
        }

        var busy = await db.Bookings
            .AsNoTracking()
            .Where(b => b.MasterId == masterId && b.Date == date && b.Status != BookingStatus.Cancelled)
            .Select(b => new TimeRange(b.StartTime, b.EndTime))
            .ToListAsync(ct);

        return SlotCalculator.GetFreeSlots(
            new TimeRange(hours.StartTime, hours.EndTime),
            busy,
            service.DurationMinutes,
            _options.SlotStepMinutes,
            notBefore: date == today ? now : null);
    }

    /// <summary>Current date and time in the salon's time zone.</summary>
    public (DateOnly Today, TimeOnly Now) GetSalonNow()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);
        var local = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime;
        return (DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
    }
}
