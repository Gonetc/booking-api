namespace BookingApi.Domain;

/// <summary>A service offered by the salon, e.g. "Men's haircut".</summary>
public class SalonService
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }

    /// <summary>Inactive services are hidden from clients but kept for booking history.</summary>
    public bool IsActive { get; set; } = true;

    public List<Master> Masters { get; set; } = [];
}
