namespace BookingApi.Options;

public class SalonOptions
{
    public const string SectionName = "Salon";

    /// <summary>IANA time zone of the salon, e.g. "Europe/Berlin". All bookings are stored in this local time.</summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>Distance between possible booking start times, in minutes.</summary>
    public int SlotStepMinutes { get; set; } = 30;
}
