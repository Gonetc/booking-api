namespace BookingApi.Domain;

public enum BookingStatus
{
    Confirmed,
    Cancelled,
    Completed
}

/// <summary>A client's appointment with a master. Date and times are in salon local time.</summary>
public class Booking
{
    public int Id { get; set; }

    public int MasterId { get; set; }
    public Master Master { get; set; } = null!;

    public int ServiceId { get; set; }
    public SalonService Service { get; set; } = null!;

    public required string ClientName { get; set; }
    public required string ClientPhone { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime CreatedAtUtc { get; set; }
}
