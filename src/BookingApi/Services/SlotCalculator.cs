namespace BookingApi.Services;

public readonly record struct TimeRange(TimeOnly Start, TimeOnly End);

/// <summary>
/// Pure scheduling logic with no database access, so it is easy to unit-test.
/// </summary>
public static class SlotCalculator
{
    /// <summary>
    /// Returns start times at which an appointment of <paramref name="durationMinutes"/>
    /// fits into <paramref name="workingHours"/> without overlapping any <paramref name="busy"/> range.
    /// </summary>
    /// <param name="workingHours">Master's working time for the day.</param>
    /// <param name="busy">Already booked ranges (end is exclusive).</param>
    /// <param name="durationMinutes">Length of the requested service.</param>
    /// <param name="stepMinutes">Grid of possible start times, counted from the opening time.</param>
    /// <param name="notBefore">Optional lower bound, e.g. "now" when the date is today.</param>
    public static IReadOnlyList<TimeOnly> GetFreeSlots(
        TimeRange workingHours,
        IEnumerable<TimeRange> busy,
        int durationMinutes,
        int stepMinutes,
        TimeOnly? notBefore = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationMinutes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepMinutes);

        var open = ToMinutes(workingHours.Start);
        var close = ToMinutes(workingHours.End);
        var earliest = notBefore is { } nb ? ToMinutes(nb) : int.MinValue;
        var busyRanges = busy.Select(b => (Start: ToMinutes(b.Start), End: ToMinutes(b.End))).ToList();

        var result = new List<TimeOnly>();
        for (var start = open; start + durationMinutes <= close; start += stepMinutes)
        {
            if (start < earliest)
            {
                continue;
            }

            var end = start + durationMinutes;
            var overlaps = busyRanges.Any(b => start < b.End && b.Start < end);
            if (!overlaps)
            {
                result.Add(FromMinutes(start));
            }
        }

        return result;
    }

    private static int ToMinutes(TimeOnly t) => t.Hour * 60 + t.Minute;

    private static TimeOnly FromMinutes(int minutes) => new(minutes / 60, minutes % 60);
}
