using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
public class EnrolmentsController(RaceDayDbContext db, AccessService access, TimeProvider clock) : ControllerBase
{
    [HttpPost("api/events/{eventId:int}/enrolments"), Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> Create(int eventId, EnrolmentRequest request)
    {
        var race = await db.Events.FindAsync(eventId) ?? throw new ApiException(404, "Event not found.");
        BeforeStart(race);
        var category = await access.CategoryForEvent(request.CategoryId, eventId);
        if (await db.Enrolments.AnyAsync(x => x.EventId == eventId && x.ParticipantId == access.UserId))
            throw new ApiException(409, "You are already enrolled in this event.");
        var enrolment = new Enrolment { EventId = eventId, ParticipantId = access.UserId,
            CategoryId = category.CategoryId, Event = race, Category = category,
            Participant = (await db.Users.FindAsync(access.UserId))! };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        return Created("/api/users/me/enrolments", Responses.Enrolment(enrolment));
    }

    [HttpGet("api/users/me/enrolments"), Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> GetMine() => Ok((await Query()
        .Where(x => x.ParticipantId == access.UserId).OrderBy(x => x.EnrolmentId).ToListAsync()).Select(Responses.Enrolment));

    [HttpGet("api/events/{eventId:int}/enrolments"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        await access.OwnedEvent(eventId);
        return Ok((await Query().Where(x => x.EventId == eventId).OrderBy(x => x.EnrolmentId)
            .ToListAsync()).Select(Responses.Enrolment));
    }

    [HttpPut("api/enrolments/{enrolmentId:int}"), Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> Update(int enrolmentId, EnrolmentRequest request)
    {
        var enrolment = await access.OwnEnrolment(enrolmentId);
        BeforeStart(enrolment.Event);
        if (enrolment.Result is not null) throw new ApiException(409, "An enrolment with a result cannot be changed.");
        enrolment.Category = await access.CategoryForEvent(request.CategoryId, enrolment.EventId);
        enrolment.CategoryId = request.CategoryId;
        await db.SaveChangesAsync();
        return Ok(Responses.Enrolment(enrolment));
    }

    [HttpDelete("api/enrolments/{enrolmentId:int}"), Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> Delete(int enrolmentId)
    {
        var enrolment = await access.OwnEnrolment(enrolmentId);
        BeforeStart(enrolment.Event);
        if (enrolment.Result is not null) throw new ApiException(409, "An enrolment with a result cannot be cancelled.");
        db.Enrolments.Remove(enrolment);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<Enrolment> Query() => db.Enrolments.AsNoTracking()
        .Include(x => x.Event).Include(x => x.Category).Include(x => x.Participant);
    private void BeforeStart(RaceEvent race)
    {
        if (race.EventDate <= clock.GetUtcNow().UtcDateTime)
            throw new ApiException(409, "The event has started; enrolments can no longer be created, changed or cancelled.");
    }
}
