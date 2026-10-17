using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class DatabaseTests
{
    [Theory]
    [InlineData("invalid-role")]
    [InlineData("duplicate-email")]
    [InlineData("invalid-event-type")]
    [InlineData("negative-distance")]
    [InlineData("missing-organiser")]
    [InlineData("invalid-age-category")]
    [InlineData("invalid-distance-category")]
    [InlineData("invalid-status")]
    [InlineData("duplicate-enrolment")]
    [InlineData("duplicate-result")]
    [InlineData("invalid-position")]
    public async Task Database_rejects_invalid_direct_writes_without_API_validation(string violation)
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        using var participant = await app.Actor("Participant");
        var eventId = await app.Event(organiser);
        var categoryId = await app.Category(organiser, eventId);
        var enrolmentId = await app.Enrol(participant, eventId, categoryId);
        await app.WithDb(async db =>
        {
            var race = await db.Events.SingleAsync();
            var enrolment = await db.Enrolments.SingleAsync();
            switch (violation)
            {
                case "invalid-role": (await db.Users.FirstAsync()).Role = "Admin"; break;
                case "duplicate-email":
                    var email = (await db.Users.FirstAsync()).Email;
                    db.Users.Add(new User { FirstName = "A", LastName = "B", Email = email, Role = "Participant", PasswordHash = "test-only" }); break;
                case "invalid-event-type": db.EventTypes.Add(new EventType { Name = "Swim" }); break;
                case "negative-distance": race.DistanceKm = -1; break;
                case "missing-organiser": race.OrganiserId = int.MaxValue; break;
                case "invalid-age-category": db.Categories.Add(new Category { EventId = eventId, CategoryType = "Age", Name = "Invalid", MinAge = 40, MaxAge = 20 }); break;
                case "invalid-distance-category": db.Categories.Add(new Category { EventId = eventId, CategoryType = "Distance", Name = "Invalid", DistanceKm = 0 }); break;
                case "invalid-status": enrolment.EnrolmentStatus = "Unknown"; break;
                case "duplicate-enrolment": db.Enrolments.Add(new Enrolment { EventId = eventId, ParticipantId = enrolment.ParticipantId, CategoryId = categoryId }); break;
                case "duplicate-result":
                    db.Results.Add(new RaceResult { EnrolmentId = enrolmentId, FinishTime = TimeSpan.FromMinutes(30), FinishingPosition = 1 });
                    await db.SaveChangesAsync();
                    db.ChangeTracker.Clear();
                    db.Results.Add(new RaceResult { EnrolmentId = enrolmentId, FinishTime = TimeSpan.FromMinutes(31), FinishingPosition = 2 }); break;
                case "invalid-position": db.Results.Add(new RaceResult { EnrolmentId = enrolmentId, FinishTime = TimeSpan.FromMinutes(30), FinishingPosition = 0 }); break;
            }
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task Event_delete_cascades_unused_categories_and_enrolment_delete_cascades_result()
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        using var participant = await app.Actor("Participant");
        var unused = await app.Event(organiser);
        await app.Category(organiser, unused);
        var used = await app.Event(organiser);
        var category = await app.Category(organiser, used);
        var enrolmentId = await app.Enrol(participant, used, category);
        await app.WithDb(async db =>
        {
            db.Results.Add(new RaceResult { EnrolmentId = enrolmentId, FinishTime = TimeSpan.FromMinutes(30), FinishingPosition = 1 });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            db.Events.Remove((await db.Events.FindAsync(unused))!);
            await db.SaveChangesAsync();
            Assert.False(await db.Categories.AnyAsync(x => x.EventId == unused));
            db.Enrolments.Remove((await db.Enrolments.FindAsync(enrolmentId))!);
            await db.SaveChangesAsync();
            Assert.Empty(await db.Results.ToListAsync());
        });
    }
}
