using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class ResultTests
{
    [Fact]
    public async Task Only_event_owner_captures_and_corrects_results_and_participants_see_only_own_history()
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        using var otherOrganiser = await app.Actor();
        using var participant = await app.Actor("Participant");
        using var otherParticipant = await app.Actor("Participant");
        var eventId = await app.Event(organiser);
        var category = await app.Category(organiser, eventId);
        var enrolmentId = await app.Enrol(participant, eventId, category);
        var body = new { finishTime = "00:52:18", finishingPosition = 47 };
        Assert.Equal(HttpStatusCode.Conflict, (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", body)).StatusCode);
        app.Clock.Now = app.Clock.Now.AddDays(2);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOrganiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", body)).StatusCode);
        var created = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var resultId = (await ApiFactory.Json(created)).GetProperty("resultId").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOrganiser.PutAsJsonAsync($"/api/results/{resultId}", body)).StatusCode);
        var update = await organiser.PutAsJsonAsync($"/api/results/{resultId}", new { finishTime = "00:51:59", finishingPosition = 46 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("00:51:59", (await ApiFactory.Json(update)).GetProperty("finishTime").GetString());
        var ownHistory = await ApiFactory.Json(await participant.GetAsync("/api/users/me/results"));
        Assert.Equal(1, ownHistory.GetArrayLength());
        Assert.Equal("Polokwane Community Run", ownHistory[0].GetProperty("eventName").GetString());
        Assert.Equal("10km Open", ownHistory[0].GetProperty("categoryName").GetString());
        Assert.Equal(46, ownHistory[0].GetProperty("finishingPosition").GetInt32());
        Assert.Equal(0, (await ApiFactory.Json(await otherParticipant.GetAsync("/api/users/me/results"))).GetArrayLength());
        Assert.Equal(1, (await ApiFactory.Json(await organiser.GetAsync($"/api/events/{eventId}/results"))).GetArrayLength());
    }

    [Theory]
    [InlineData("-00:01:00", 1)]
    [InlineData("1.00:00:00", 1)]
    [InlineData("00:01:00.5", 1)]
    [InlineData("00:52:18", 0)]
    [InlineData(null, 1)]
    [InlineData("not-a-time", 1)]
    public async Task Invalid_result_returns_400(string? finishTime, int finishingPosition)
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        var response = await organiser.PostAsJsonAsync("/api/enrolments/999/result", new { finishTime, finishingPosition });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Missing_result_or_enrolment_returns_404()
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        var body = new { finishTime = "00:52:18", finishingPosition = 47 };
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.PostAsJsonAsync("/api/enrolments/999/result", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.PutAsJsonAsync("/api/results/999", body)).StatusCode);
    }
}
