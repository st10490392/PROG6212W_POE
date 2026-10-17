using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EnrolmentTests
{
    [Fact]
    public async Task Participant_enrols_changes_category_views_own_list_and_cancels()
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        using var participant = await app.Actor("Participant");
        using var other = await app.Actor("Participant");
        var eventId = await app.Event(organiser);
        var category = await app.Category(organiser, eventId);
        var alternate = await app.Category(organiser, eventId);
        var id = await app.Enrol(participant, eventId, category);
        Assert.Equal(1, (await ApiFactory.Json(await participant.GetAsync("/api/users/me/enrolments"))).GetArrayLength());
        Assert.Equal(0, (await ApiFactory.Json(await other.GetAsync("/api/users/me/enrolments"))).GetArrayLength());
        Assert.Equal(1, (await ApiFactory.Json(await organiser.GetAsync($"/api/events/{eventId}/enrolments"))).GetArrayLength());
        var update = await participant.PutAsJsonAsync($"/api/enrolments/{id}", new { categoryId = alternate });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(alternate, (await ApiFactory.Json(update)).GetProperty("categoryId").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/enrolments/{id}", new { categoryId = alternate })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/enrolments/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await participant.DeleteAsync($"/api/enrolments/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await participant.DeleteAsync($"/api/enrolments/{id}")).StatusCode);
        // Cancellation physically removes the record, allowing a participant to enter again.
        await app.Enrol(participant, eventId, category);
    }

    [Fact]
    public async Task Duplicate_enrolment_returns_409_and_missing_or_wrong_event_category_is_rejected()
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        using var participant = await app.Actor("Participant");
        var eventId = await app.Event(organiser);
        var otherEvent = await app.Event(organiser);
        var category = await app.Category(organiser, eventId);
        var wrongCategory = await app.Category(organiser, otherEvent);
        Assert.Equal(HttpStatusCode.BadRequest, (await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = wrongCategory })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = 999 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await participant.PostAsJsonAsync("/api/events/999/enrolments", new { categoryId = category })).StatusCode);
        var id = await app.Enrol(participant, eventId, category);
        Assert.Equal(HttpStatusCode.Conflict, (await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = category })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await participant.PutAsJsonAsync($"/api/enrolments/{id}", new { categoryId = wrongCategory })).StatusCode);
    }

    [Fact]
    public async Task Enrolment_create_change_and_cancel_close_at_exact_start_time()
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        using var participant = await app.Actor("Participant");
        using var lateParticipant = await app.Actor("Participant");
        var eventId = await app.Event(organiser);
        var category = await app.Category(organiser, eventId);
        var id = await app.Enrol(participant, eventId, category);
        app.Clock.Now = app.Clock.Now.AddDays(1);
        Assert.Equal(HttpStatusCode.Conflict, (await participant.PutAsJsonAsync($"/api/enrolments/{id}", new { categoryId = category })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await participant.DeleteAsync($"/api/enrolments/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await lateParticipant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = category })).StatusCode);
    }
}
