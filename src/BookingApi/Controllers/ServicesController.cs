using BookingApi.Auth;
using BookingApi.Contracts;
using BookingApi.Data;
using BookingApi.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Controllers;

[ApiController]
[Route("api/services")]
[Produces("application/json")]
public class ServicesController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists salon services. Inactive ones are hidden unless requested.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<ServiceResponse>> GetAll(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var services = await db.Services
            .AsNoTracking()
            .Where(s => includeInactive || s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return services.Select(s => s.ToResponse()).ToList();
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceResponse>> Get(int id, CancellationToken ct)
    {
        var service = await db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null)
        {
            return NotFound();
        }

        return service.ToResponse();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceResponse>> Create(UpsertServiceRequest request, CancellationToken ct)
    {
        var service = new SalonService { Name = request.Name };
        Apply(service, request);

        db.Services.Add(service);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = service.Id }, service.ToResponse());
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceResponse>> Update(int id, UpsertServiceRequest request, CancellationToken ct)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null)
        {
            return NotFound();
        }

        Apply(service, request);
        await db.SaveChangesAsync(ct);

        return service.ToResponse();
    }

    /// <summary>Soft delete: the service is deactivated so booking history stays intact.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null)
        {
            return NotFound();
        }

        service.IsActive = false;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static void Apply(SalonService service, UpsertServiceRequest request)
    {
        service.Name = request.Name.Trim();
        service.Description = request.Description?.Trim();
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;
        service.IsActive = request.IsActive;
    }
}
