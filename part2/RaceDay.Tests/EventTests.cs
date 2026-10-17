using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EventTests
{
    [Theory]
    [InlineData(400)]
    [InlineData(600)]
    public async Task Create_and_update_responses_match_persisted_event_date_precision(int milliseconds)
    {
        await using var app = new ApiFactory();
        using var owner = await app.Actor();
        var created = await owner.PostAsJsonAsync("/api/events", app.EventBody(date: app.Clock.Now.AddDays(1).AddMilliseconds(milliseconds)));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdJson = await ApiFactory.Json(created);
        var route = $"/api/events/{createdJson.GetProperty("eventId").GetInt32()}";
        var read = await owner.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var createdDate = createdJson.GetProperty("eventDate").GetDateTimeOffset();
        Assert.Equal(createdDate, (await ApiFactory.Json(read)).GetProperty("eventDate").GetDateTimeOffset());

        var updated = await owner.PutAsJsonAsync(route, app.EventBody(date: app.Clock.Now.AddDays(2).AddMilliseconds(milliseconds)));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedDate = (await ApiFactory.Json(updated)).GetProperty("eventDate").GetDateTimeOffset();
        var reread = await owner.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, reread.StatusCode);
        Assert.Equal(updatedDate, (await ApiFactory.Json(reread)).GetProperty("eventDate").GetDateTimeOffset());
        if (app.UsesSqlServer)
        {
            Assert.Equal(0L, createdDate.Ticks % TimeSpan.TicksPerSecond);
            Assert.Equal(0L, updatedDate.Ticks % TimeSpan.TicksPerSecond);
        }
    }

    [Fact]
    public async Task Organiser_creates_reads_updates_and_deletes_own_event()
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        using var visitor = app.Browser();
        var id = await app.Event(organiser);
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync($"/api/events/{id}")).StatusCode);
        var update = await organiser.PutAsJsonAsync($"/api/events/{id}", app.EventBody(2));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Walk", (await ApiFactory.Json(update)).GetProperty("eventType").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await organiser.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Public_event_list_filters_upcoming_events_by_type_and_UTC_date()
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        using var visitor = app.Browser();
        await app.Event(organiser);
        await organiser.PostAsJsonAsync("/api/events", app.EventBody(2));
        await organiser.PostAsJsonAsync("/api/events", app.EventBody(1, app.Clock.Now.AddDays(-1)));
        Assert.Equal(2, (await ApiFactory.Json(await visitor.GetAsync("/api/events"))).GetArrayLength());
        Assert.Equal(1, (await ApiFactory.Json(await visitor.GetAsync("/api/events?eventTypeId=2&date=2030-06-02"))).GetArrayLength());
        Assert.Equal(0, (await ApiFactory.Json(await visitor.GetAsync("/api/events?date=2030-06-03"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync("/api/events?date=invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync("/api/events?eventTypeId=-1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync("/api/events?date=9999-12-31")).StatusCode);
        var types = await ApiFactory.Json(await visitor.GetAsync("/api/event-types"));
        Assert.Equal(new[] { "Run", "Walk", "Cycle" }, types.EnumerateArray().Select(x => x.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task Another_organiser_cannot_change_delete_or_view_private_event_data()
    {
        await using var app = new ApiFactory();
        using var owner = await app.Actor();
        using var other = await app.Actor();
        var id = await app.Event(owner);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/events/{id}", app.EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/events/{id}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/events/{id}/results")).StatusCode);
    }

    [Fact]
    public async Task Invalid_event_type_and_payload_are_rejected_and_missing_event_returns_404()
    {
        await using var app = new ApiFactory();
        using var owner = await app.Actor();
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/events", app.EventBody(999))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/events", new { name = "", distanceKm = -1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PutAsJsonAsync("/api/events/999", app.EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync("/api/events/999")).StatusCode);
    }

    [Fact]
    public async Task Enrolled_event_and_used_category_cannot_be_deleted()
    {
        await using var app = new ApiFactory();
        using var owner = await app.Actor();
        using var participant = await app.Actor("Participant");
        var id = await app.Event(owner);
        var categoryId = await app.Category(owner, id);
        await app.Enrol(participant, id, categoryId);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/events/{id}")).StatusCode);
    }
}
