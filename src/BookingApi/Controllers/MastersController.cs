using BookingApi.Auth;
using BookingApi.Contracts;
using BookingApi.Data;
using BookingApi.Domain;
using BookingApi.Errors;
using BookingApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Controllers;

[ApiController]
[Route("api/masters")]
[Produces("application/json")]
public class MastersController(AppDbContext db, AvailabilityService availability) : ControllerBase
{
    /// <summary>Lists active masters, optionally only those who provide a given service.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<MasterResponse>> GetAll([FromQuery] int? serviceId, CancellationToken ct)
    {
        var masters = await db.Masters
            .AsNoTracking()
            .Include(m => m.Services)
            .Include(m => m.WorkingHours)
            .Where(m => m.IsActive)
            .Where(m => serviceId == null || m.Services.Any(s => s.Id == serviceId))
            .OrderBy(m => m.Name)
            .ToListAsync(ct);

        return masters.Select(m => m.ToResponse()).ToList();
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<MasterResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MasterResponse>> Get(int id, CancellationToken ct)
    {
        var master = await LoadMasterAsync(id, tracking: false, ct);
        if (master is null)
        {
            return NotFound();
        }

        return master.ToResponse();
    }

    /// <summary>Free start times of a master for a service on a given date (salon local time).</summary>
    [HttpGet("{id:int}/availability")]
    [ProducesResponseType<AvailabilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<AvailabilityResponse> GetAvailability(
        int id,
        [FromQuery, BindRequired] int serviceId,
        [FromQuery, BindRequired] DateOnly date,
        CancellationToken ct)
    {
        var slots = await availability.GetFreeSlotsAsync(id, serviceId, date, ct);
        return new AvailabilityResponse(id, serviceId, date, slots);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ProducesResponseType<MasterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MasterResponse>> Create(UpsertMasterRequest request, CancellationToken ct)
    {
        var master = new Master { Name = request.Name };
        await ApplyAsync(master, request, ct);

        db.Masters.Add(master);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = master.Id }, master.ToResponse());
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    [ProducesResponseType<MasterResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MasterResponse>> Update(int id, UpsertMasterRequest request, CancellationToken ct)
    {
        var master = await LoadMasterAsync(id, tracking: true, ct);
        if (master is null)
        {
            return NotFound();
        }

        await ApplyAsync(master, request, ct);
        await db.SaveChangesAsync(ct);

        return master.ToResponse();
    }

    /// <summary>Soft delete: the master is deactivated so booking history stays intact.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var master = await db.Masters.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (master is null)
        {
            return NotFound();
        }

        master.IsActive = false;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private Task<Master?> LoadMasterAsync(int id, bool tracking, CancellationToken ct)
    {
        var query = db.Masters.Include(m => m.Services).Include(m => m.WorkingHours).AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    private async Task ApplyAsync(Master master, UpsertMasterRequest request, CancellationToken ct)
    {
        ValidateWorkingHours(request.WorkingHours);

        var serviceIds = request.ServiceIds.Distinct().ToArray();
        var services = await db.Services.Where(s => serviceIds.Contains(s.Id)).ToListAsync(ct);
        if (services.Count != serviceIds.Length)
        {
            throw new BusinessRuleException("One or more services do not exist.");
        }

        master.Name = request.Name.Trim();
        master.Specialization = request.Specialization?.Trim();
        master.PhotoUrl = request.PhotoUrl;
        master.IsActive = request.IsActive;

        master.Services.Clear();
        master.Services.AddRange(services);

        master.WorkingHours.Clear();
        master.WorkingHours.AddRange(request.WorkingHours.Select(h => new WorkingHours
        {
            DayOfWeek = h.DayOfWeek,
            StartTime = h.StartTime,
            EndTime = h.EndTime
        }));
    }

    private static void ValidateWorkingHours(IReadOnlyList<WorkingHoursDto> hours)
    {
        if (hours.Any(h => h.StartTime >= h.EndTime))
        {
            throw new BusinessRuleException("Working hours must start before they end.");
        }

        if (hours.GroupBy(h => h.DayOfWeek).Any(g => g.Count() > 1))
        {
            throw new BusinessRuleException("Each day of week can appear only once in working hours.");
        }
    }
}
