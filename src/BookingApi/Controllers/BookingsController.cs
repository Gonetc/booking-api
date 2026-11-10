using BookingApi.Auth;
using BookingApi.Contracts;
using BookingApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[ApiController]
[Route("api/bookings")]
[Produces("application/json")]
public class BookingsController(BookingService bookings) : ControllerBase
{
    /// <summary>Lists bookings, optionally filtered by date and master.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IReadOnlyList<BookingResponse>> GetAll(
        [FromQuery] DateOnly? date, [FromQuery] int? masterId, CancellationToken ct)
    {
        var list = await bookings.ListAsync(date, masterId, ct);
        return list.Select(b => b.ToResponse()).ToList();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("{id:int}")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<BookingResponse> Get(int id, CancellationToken ct) =>
        (await bookings.GetAsync(id, ct)).ToResponse();

    /// <summary>Books a free slot. Returns 409 if the slot is already taken or in the past.</summary>
    [HttpPost]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var booking = await bookings.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = booking.Id }, booking.ToResponse());
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<BookingResponse> Cancel(int id, CancellationToken ct) =>
        (await bookings.CancelAsync(id, ct)).ToResponse();
}
