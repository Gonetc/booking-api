namespace BookingApi.Domain;

/// <summary>A specialist (barber, stylist, ...) who performs services.</summary>
public class Master
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Specialization { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Services this master is able to perform.</summary>
    public List<SalonService> Services { get; set; } = [];

    /// <summary>Weekly schedule: at most one entry per day of week.</summary>
    public List<WorkingHours> WorkingHours { get; set; } = [];
}
