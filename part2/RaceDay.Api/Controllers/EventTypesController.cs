using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/event-types")]
public class EventTypesController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await db.EventTypes.AsNoTracking()
        .OrderBy(x => x.EventTypeId).Select(x => new { x.EventTypeId, x.Name }).ToListAsync());
}
