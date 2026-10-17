using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RACEDAY_TEST_SQLSERVER")))
            Skip = "Set RACEDAY_TEST_SQLSERVER to run SQL Server migration/type compatibility checks.";
    }
}

public class SqlServerTests
{
    [SqlServerFact]
    public async Task Real_SQL_Server_applies_migration_and_round_trips_SQL_types()
    {
        await using var app = new ApiFactory();
        Assert.True(app.UsesSqlServer);
        using var organiser = await app.Actor();
        using var participant = await app.Actor("Participant");
        var eventId = await app.Event(organiser);
        var categoryId = await app.Category(organiser, eventId);
        var enrolmentId = await app.Enrol(participant, eventId, categoryId);
        await app.WithDb(async db =>
        {
            Assert.True(db.Database.IsSqlServer());
            Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
            var user = await db.Users.SingleAsync(x => x.Role == "Participant");
            user.DateOfBirth = new DateOnly(2001, 6, 20);
            db.Results.Add(new RaceResult { EnrolmentId = enrolmentId, FinishTime = new TimeSpan(0, 52, 18), FinishingPosition = 47 });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Equal(new DateOnly(2001, 6, 20), (await db.Users.FindAsync(user.UserId))!.DateOfBirth);
            Assert.Equal(10.00m, (await db.Events.FindAsync(eventId))!.DistanceKm);
            Assert.Equal(new TimeSpan(0, 52, 18), (await db.Results.SingleAsync()).FinishTime);
        });
    }

    [SqlServerFact]
    public async Task Real_SQL_Server_enforces_NVARCHAR_length_without_API_validation()
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        await app.WithDb(async db =>
        {
            (await db.Users.SingleAsync()).FirstName = new string('A', 51);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
    }
}
