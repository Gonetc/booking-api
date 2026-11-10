using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BookingApi.Contracts;
using BookingApi.Domain;

namespace BookingApi.Tests;

/// <summary>End-to-end tests through HTTP: catalog → availability → booking → cancellation.</summary>
public class BookingFlowTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // Monday (seed: Alex works Mon–Fri 10:00–19:00) and Sunday (nobody works)
    private static readonly DateOnly Monday = DateOnly.FromDateTime(BookingApiFactory.Now.UtcDateTime);
    private static readonly DateOnly Sunday = Monday.AddDays(-1);

    private readonly BookingApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Services_ReturnsSeededCatalog()
    {
        var services = await _client.GetFromJsonAsync<List<ServiceResponse>>("/api/services", Json);

        Assert.NotNull(services);
        Assert.Equal(3, services.Count);
    }

    [Fact]
    public async Task Availability_OnWorkingDay_StartsAtOpeningTime()
    {
        var (masterId, serviceId) = await GetAlexAndHaircutAsync();

        var availability = await GetAvailabilityAsync(masterId, serviceId, Monday);

        // 10:00–19:00, 60-minute service, 30-minute step => 10:00 ... 18:00
        Assert.Equal(17, availability.Slots.Count);
        Assert.Equal(new TimeOnly(10, 0), availability.Slots[0]);
        Assert.Equal(new TimeOnly(18, 0), availability.Slots[^1]);
    }

    [Fact]
    public async Task Availability_OnDayOff_IsEmpty()
    {
        var (masterId, serviceId) = await GetAlexAndHaircutAsync();

        var availability = await GetAvailabilityAsync(masterId, serviceId, Sunday.AddDays(7));

        Assert.Empty(availability.Slots);
    }

    [Fact]
    public async Task Booking_TakesTheSlot_AndSecondBookingOfSameSlotFails()
    {
        var (masterId, serviceId) = await GetAlexAndHaircutAsync();
        var request = NewBooking(masterId, serviceId, Monday, new TimeOnly(12, 0));

        var first = await _client.PostAsJsonAsync("/api/bookings", request, Json);
        var second = await _client.PostAsJsonAsync("/api/bookings", request, Json);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var availability = await GetAvailabilityAsync(masterId, serviceId, Monday);
        Assert.DoesNotContain(new TimeOnly(11, 30), availability.Slots); // would overlap 12:00–13:00
        Assert.DoesNotContain(new TimeOnly(12, 0), availability.Slots);
        Assert.DoesNotContain(new TimeOnly(12, 30), availability.Slots);
        Assert.Contains(new TimeOnly(13, 0), availability.Slots);
    }

    [Fact]
    public async Task CancelledBooking_FreesTheSlot()
    {
        var (masterId, serviceId) = await GetAlexAndHaircutAsync();
        var created = await _client.PostAsJsonAsync(
            "/api/bookings", NewBooking(masterId, serviceId, Monday, new TimeOnly(15, 0)), Json);
        var booking = await created.Content.ReadFromJsonAsync<BookingResponse>(Json);

        var admin = await _factory.CreateAdminClientAsync();
        var cancel = await admin.PostAsync($"/api/bookings/{booking!.Id}/cancel", null);
        var cancelled = await cancel.Content.ReadFromJsonAsync<BookingResponse>(Json);

        Assert.Equal(BookingStatus.Cancelled, cancelled!.Status);
        var availability = await GetAvailabilityAsync(masterId, serviceId, Monday);
        Assert.Contains(new TimeOnly(15, 0), availability.Slots);
    }

    [Fact]
    public async Task Booking_InThePast_IsRejected()
    {
        var (masterId, serviceId) = await GetAlexAndHaircutAsync();
        var lastFriday = Monday.AddDays(-3);

        var response = await _client.PostAsJsonAsync(
            "/api/bookings", NewBooking(masterId, serviceId, lastFriday, new TimeOnly(12, 0)), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Booking_WithInvalidPhone_ReturnsValidationError()
    {
        var (masterId, serviceId) = await GetAlexAndHaircutAsync();
        var request = NewBooking(masterId, serviceId, Monday, new TimeOnly(12, 0)) with { ClientPhone = "not a phone" };

        var response = await _client.PostAsJsonAsync("/api/bookings", request, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Booking_UnknownBooking_Returns404()
    {
        var admin = await _factory.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/bookings/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static CreateBookingRequest NewBooking(int masterId, int serviceId, DateOnly date, TimeOnly time) =>
        new(masterId, serviceId, date, time, "John Smith", "+1 555 123 4567");

    private async Task<(int MasterId, int ServiceId)> GetAlexAndHaircutAsync()
    {
        var masters = await _client.GetFromJsonAsync<List<MasterResponse>>("/api/masters", Json);
        var services = await _client.GetFromJsonAsync<List<ServiceResponse>>("/api/services", Json);

        var alex = masters!.Single(m => m.Name == "Alex");
        var haircut = services!.Single(s => s.Name == "Men's haircut");
        return (alex.Id, haircut.Id);
    }

    private async Task<AvailabilityResponse> GetAvailabilityAsync(int masterId, int serviceId, DateOnly date)
    {
        var url = $"/api/masters/{masterId}/availability?serviceId={serviceId}&date={date:yyyy-MM-dd}";
        var result = await _client.GetFromJsonAsync<AvailabilityResponse>(url, Json);
        return result!;
    }
}
