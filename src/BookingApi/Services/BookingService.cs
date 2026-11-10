using BookingApi.Contracts;
using BookingApi.Data;
using BookingApi.Domain;
using BookingApi.Errors;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Services;

public class BookingService(AppDbContext db, AvailabilityService availability, TimeProvider timeProvider)
{
    public async Task<Booking> GetAsync(int id, CancellationToken ct) =>
        await db.Bookings
            .AsNoTracking()
            .Include(b => b.Master)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
        ?? throw new NotFoundException($"Booking {id} was not found.");

    public async Task<IReadOnlyList<Booking>> ListAsync(DateOnly? date, int? masterId, CancellationToken ct) =>
        await db.Bookings
            .AsNoTracking()
            .Include(b => b.Master)
            .Include(b => b.Service)
            .Where(b => date == null || b.Date == date)
            .Where(b => masterId == null || b.MasterId == masterId)
            .OrderBy(b => b.Date)
            .ThenBy(b => b.StartTime)
            .ToListAsync(ct);

    public async Task<Booking> CreateAsync(CreateBookingRequest request, CancellationToken ct)
    {
        // Validates master, service and date, and returns only really free start times
        var freeSlots = await availability.GetFreeSlotsAsync(request.MasterId, request.ServiceId, request.Date, ct);
        if (!freeSlots.Contains(request.StartTime))
        {
            throw new ConflictException("The selected time slot is not available.");
        }

        var service = await db.Services.FirstAsync(s => s.Id == request.ServiceId, ct);

        var booking = new Booking
        {
            MasterId = request.MasterId,
            ServiceId = request.ServiceId,
            ClientName = request.ClientName.Trim(),
            ClientPhone = request.ClientPhone.Trim(),
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.StartTime.AddMinutes(service.DurationMinutes),
            Status = BookingStatus.Confirmed,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);

        return await GetAsync(booking.Id, ct);
    }

    public async Task<Booking> CancelAsync(int id, CancellationToken ct)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new NotFoundException($"Booking {id} was not found.");

        if (booking.Status == BookingStatus.Completed)
        {
            throw new BusinessRuleException("A completed booking cannot be cancelled.");
        }

        if (booking.Status != BookingStatus.Cancelled)
        {
            booking.Status = BookingStatus.Cancelled;
            await db.SaveChangesAsync(ct);
        }

        return await GetAsync(id, ct);
    }
}
