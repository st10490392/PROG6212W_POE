using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public sealed class AccessService(RaceDayDbContext db, IHttpContextAccessor http)
{
    public int UserId => int.Parse(http.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<RaceEvent> OwnedEvent(int eventId)
    {
        var race = await db.Events.Include(x => x.EventType).Include(x => x.Categories)
            .SingleOrDefaultAsync(x => x.EventId == eventId)
            ?? throw new ApiException(404, "Event not found.");
        if (race.OrganiserId != UserId) throw new ApiException(403, "This event belongs to another organiser.");
        return race;
    }

    public async Task<Enrolment> OwnEnrolment(int enrolmentId)
    {
        var enrolment = await Enrolment(enrolmentId);
        if (enrolment.ParticipantId != UserId) throw new ApiException(403, "This enrolment belongs to another participant.");
        return enrolment;
    }

    public async Task<Enrolment> Enrolment(int enrolmentId) =>
        await db.Enrolments.Include(x => x.Event).Include(x => x.Category).Include(x => x.Participant)
            .Include(x => x.Result).SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId)
        ?? throw new ApiException(404, "Enrolment not found.");

    public async Task<Category> CategoryForEvent(int categoryId, int eventId)
    {
        var category = await db.Categories.FindAsync(categoryId)
            ?? throw new ApiException(404, "Category not found.");
        if (category.EventId != eventId) throw new ApiException(400, "Category must belong to this event.");
        return category;
    }
}
