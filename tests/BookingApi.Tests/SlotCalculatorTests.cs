using BookingApi.Services;

namespace BookingApi.Tests;

public class SlotCalculatorTests
{
    private static readonly TimeRange Day = new(new TimeOnly(10, 0), new TimeOnly(13, 0));

    private static TimeOnly T(int hour, int minute = 0) => new(hour, minute);

    [Fact]
    public void EmptyDay_ReturnsEveryStepThatFitsBeforeClosing()
    {
        var slots = SlotCalculator.GetFreeSlots(Day, [], durationMinutes: 60, stepMinutes: 30);

        Assert.Equal(new[] { T(10), T(10, 30), T(11), T(11, 30), T(12) }, slots);
    }

    [Fact]
    public void Slot_ThatWouldEndAfterClosing_IsNotOffered()
    {
        var slots = SlotCalculator.GetFreeSlots(Day, [], durationMinutes: 90, stepMinutes: 30);

        Assert.DoesNotContain(T(12), slots);
        Assert.Equal(T(11, 30), slots[^1]);
    }

    [Fact]
    public void Slots_OverlappingABooking_AreExcluded()
    {
        TimeRange[] busy = [new(T(11), T(12))];

        var slots = SlotCalculator.GetFreeSlots(Day, busy, durationMinutes: 60, stepMinutes: 30);

        Assert.Equal(new[] { T(10), T(12) }, slots);
    }

    [Fact]
    public void Booking_RightAfterAnotherBooking_IsAllowed()
    {
        TimeRange[] busy = [new(T(10), T(11))];

        var slots = SlotCalculator.GetFreeSlots(Day, busy, durationMinutes: 60, stepMinutes: 60);

        Assert.Equal(new[] { T(11), T(12) }, slots);
    }

    [Fact]
    public void NotBefore_HidesSlotsInThePast()
    {
        var slots = SlotCalculator.GetFreeSlots(Day, [], durationMinutes: 30, stepMinutes: 30, notBefore: T(11, 10));

        Assert.Equal(new[] { T(11, 30), T(12), T(12, 30) }, slots);
    }

    [Fact]
    public void ServiceLongerThanWorkingDay_ReturnsNoSlots()
    {
        var slots = SlotCalculator.GetFreeSlots(Day, [], durationMinutes: 240, stepMinutes: 30);

        Assert.Empty(slots);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(30, 0)]
    public void NonPositiveDurationOrStep_Throws(int duration, int step)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SlotCalculator.GetFreeSlots(Day, [], duration, step));
    }
}
