using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
public class ResultsController(RaceDayDbContext db, AccessService access, TimeProvider clock) : ControllerBase
{
    [HttpPost("api/enrolments/{enrolmentId:int}/result"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Create(int enrolmentId, ResultRequest request)
    {
        var enrolment = await access.Enrolment(enrolmentId);
        await access.OwnedEvent(enrolment.EventId);
        if (enrolment.Result is not null) throw new ApiException(409, "A result is already recorded.");
        AfterStart(enrolment.Event);
        var result = new RaceResult { Enrolment = enrolment, EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime!.Value, FinishingPosition = request.FinishingPosition };
        db.Results.Add(result);
        await db.SaveChangesAsync();
        return Created($"/api/events/{enrolment.EventId}/results", Responses.Result(result));
    }

    [HttpPut("api/results/{resultId:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Update(int resultId, ResultRequest request)
    {
        var result = await Query().SingleOrDefaultAsync(x => x.ResultId == resultId)
            ?? throw new ApiException(404, "Result not found.");
        await access.OwnedEvent(result.Enrolment.EventId);
        AfterStart(result.Enrolment.Event);
        result.FinishTime = request.FinishTime!.Value; result.FinishingPosition = request.FinishingPosition;
        await db.SaveChangesAsync();
        return Ok(Responses.Result(result));
    }

    [HttpGet("api/users/me/results"), Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> GetMine() => Ok((await Query().AsNoTracking()
        .Where(x => x.Enrolment.ParticipantId == access.UserId).OrderByDescending(x => x.Enrolment.Event.EventDate)
        .ToListAsync()).Select(Responses.Result));

    [HttpGet("api/events/{eventId:int}/results"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        await access.OwnedEvent(eventId);
        return Ok((await Query().AsNoTracking().Where(x => x.Enrolment.EventId == eventId)
            .OrderBy(x => x.FinishingPosition).ToListAsync()).Select(Responses.Result));
    }

    private IQueryable<RaceResult> Query() => db.Results
        .Include(x => x.Enrolment).ThenInclude(x => x.Event)
        .Include(x => x.Enrolment).ThenInclude(x => x.Category);
    private void AfterStart(RaceEvent race)
    {
        // Part 1 stores a start date but no end date. It cannot prove that an event has finished.
        if (race.EventDate > clock.GetUtcNow().UtcDateTime)
            throw new ApiException(409, "Results cannot be recorded before the event starts.");
    }
}
