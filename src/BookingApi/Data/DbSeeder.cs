using BookingApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Data;

/// <summary>Fills an empty database with demo data so the API can be tried right away.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Services.AnyAsync(ct))
        {
            return;
        }

        var haircut = new SalonService
        {
            Name = "Men's haircut",
            Description = "Classic or modern haircut, wash and styling.",
            DurationMinutes = 60,
            Price = 30
        };
        var beard = new SalonService
        {
            Name = "Beard trim",
            Description = "Beard shaping with hot towel.",
            DurationMinutes = 30,
            Price = 15
        };
        var combo = new SalonService
        {
            Name = "Haircut + beard",
            Description = "Full grooming package.",
            DurationMinutes = 90,
            Price = 40
        };

        var alex = new Master
        {
            Name = "Alex",
            Specialization = "Senior barber",
            Services = [haircut, beard, combo],
            WorkingHours = Schedule(new TimeOnly(10, 0), new TimeOnly(19, 0),
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday)
        };
        var max = new Master
        {
            Name = "Max",
            Specialization = "Barber",
            Services = [haircut, beard],
            WorkingHours = Schedule(new TimeOnly(11, 0), new TimeOnly(20, 0),
                DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday)
        };

        db.Masters.AddRange(alex, max);
        await db.SaveChangesAsync(ct);
    }

    private static List<WorkingHours> Schedule(TimeOnly start, TimeOnly end, params DayOfWeek[] days) =>
        days.Select(d => new WorkingHours { DayOfWeek = d, StartTime = start, EndTime = end }).ToList();
}
