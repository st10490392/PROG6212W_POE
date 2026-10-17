using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/events")]
public class EventsController(RaceDayDbContext db, AccessService access, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? eventTypeId, [FromQuery] DateOnly? date)
    {
        if (eventTypeId <= 0) throw new ApiException(400, "Event type ID must be positive.");
        var now = clock.GetUtcNow().UtcDateTime;
        var query = db.Events.AsNoTracking().Include(x => x.EventType).Include(x => x.Categories)
            .Where(x => x.EventDate > now);
        if (eventTypeId is not null) query = query.Where(x => x.EventTypeId == eventTypeId);
        if (date is not null)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue);
            if (date == DateOnly.MaxValue) query = query.Where(x => x.EventDate >= start);
            else
            {
                var end = start.AddDays(1);
                query = query.Where(x => x.EventDate >= start && x.EventDate < end);
            }
        }
        return Ok((await query.OrderBy(x => x.EventDate).ThenBy(x => x.EventId).ToListAsync()).Select(Responses.Event));
    }

    [HttpGet("{eventId:int}")]
    public async Task<IActionResult> GetOne(int eventId)
    {
        var race = await db.Events.AsNoTracking().Include(x => x.EventType).Include(x => x.Categories)
            .SingleOrDefaultAsync(x => x.EventId == eventId) ?? throw new ApiException(404, "Event not found.");
        return Ok(Responses.Event(race));
    }

    [HttpPost, Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        await ValidateType(request.EventTypeId);
        var race = new RaceEvent { OrganiserId = access.UserId };
        Apply(race, request);
        db.Events.Add(race);
        await db.SaveChangesAsync();
        // SQL Server rounds datetime2(0); return the persisted values, not the input's fractional seconds.
        await db.Entry(race).ReloadAsync();
        await db.Entry(race).Reference(x => x.EventType).LoadAsync();
        return CreatedAtAction(nameof(GetOne), new { eventId = race.EventId }, Responses.Event(race));
    }

    [HttpPut("{eventId:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Update(int eventId, EventRequest request)
    {
        var race = await access.OwnedEvent(eventId);
        await ValidateType(request.EventTypeId);
        Apply(race, request);
        await db.SaveChangesAsync();
        await db.Entry(race).ReloadAsync();
        await db.Entry(race).Reference(x => x.EventType).LoadAsync();
        return Ok(Responses.Event(race));
    }

    [HttpDelete("{eventId:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Delete(int eventId)
    {
        var race = await access.OwnedEvent(eventId);
        if (await db.Enrolments.AnyAsync(x => x.EventId == eventId))
            throw new ApiException(409, "Events with enrolments cannot be deleted.");
        db.Events.Remove(race);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task ValidateType(int id)
    {
        if (!await db.EventTypes.AnyAsync(x => x.EventTypeId == id)) throw new ApiException(400, "Invalid event type.");
    }
    private static void Apply(RaceEvent race, EventRequest request)
    {
        race.Name = request.Name.Trim(); race.Description = request.Description.Trim();
        race.Location = request.Location.Trim(); race.EventDate = request.EventDate.UtcDateTime;
        race.DistanceKm = request.DistanceKm; race.EventTypeId = request.EventTypeId;
    }
}
