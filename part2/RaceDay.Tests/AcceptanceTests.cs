using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace RaceDay.Tests;

public class AcceptanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Complete_demo_preserves_ownership_and_denied_writes_leave_data_unchanged()
    {
        await using var app = new ApiFactory();
        using var owner = app.Browser();
        using var other = app.Browser();
        using var participant = app.Browser();
        using var anonymous = app.Browser();
        output.WriteLine("Provider: " + (app.UsesSqlServer ? "SQL Server (isolated migrated test database)" : "SQLite (isolated relational test database)"));
        const string password = "Acceptance-Demo-Password-42";
        var suffix = Guid.NewGuid().ToString("N");
        var ownerEmail = $"owner-{suffix}@raceday.test";
        var otherEmail = $"other-{suffix}@raceday.test";
        var participantEmail = $"participant-{suffix}@raceday.test";
        async Task RegisterAndLogin(HttpClient client, string email, string role)
        {
            await Check("register " + role, await client.PostAsJsonAsync("/api/auth/register", new
            { firstName = "Demo", lastName = "Acceptance", email, password, role }), HttpStatusCode.Created);
            await Check("registration does not authenticate", await client.GetAsync("/api/users/me"), HttpStatusCode.Unauthorized);
            await Check("login " + role, await client.PostAsJsonAsync("/api/auth/login", new { email, password }), HttpStatusCode.OK);
        }
        await RegisterAndLogin(owner, ownerEmail, "Organiser");
        await RegisterAndLogin(other, otherEmail, "Organiser");
        await RegisterAndLogin(participant, participantEmail, "Participant");
        await Check("invalid known-account password", await anonymous.PostAsJsonAsync("/api/auth/login", new
        { email = ownerEmail, password = "Wrong-Demo-Password-42" }), HttpStatusCode.Unauthorized);
        await Check("invalid credentials do not authenticate", await anonymous.GetAsync("/api/users/me"), HttpStatusCode.Unauthorized);

        var eventJson = await Check("create event", await owner.PostAsJsonAsync("/api/events", app.EventBody()), HttpStatusCode.Created);
        var eventId = eventJson.GetProperty("eventId").GetInt32();
        var route = $"/api/events/{eventId}";
        var categoryJson = await Check("create Distance category", await owner.PostAsJsonAsync(route + "/categories", ApiFactory.CategoryBody), HttpStatusCode.Created);
        var categoryId = categoryJson.GetProperty("categoryId").GetInt32();
        var ageCategory = await Check("create Age category", await owner.PostAsJsonAsync(route + "/categories", new
        { name = "Adults", categoryType = "Age", minAge = 18, maxAge = 80 }), HttpStatusCode.Created);
        var ageCategoryId = ageCategory.GetProperty("categoryId").GetInt32();
        var enrolment = await Check("enrol participant", await participant.PostAsJsonAsync(route + "/enrolments", new { categoryId }), HttpStatusCode.Created);
        var enrolmentId = enrolment.GetProperty("enrolmentId").GetInt32();
        var resultRoute = $"/api/enrolments/{enrolmentId}/result";
        await Check("duplicate enrolment", await participant.PostAsJsonAsync(route + "/enrolments", new { categoryId }), HttpStatusCode.Conflict);

        var replacement = new { name = "Unauthorised replacement", description = "Denied write", location = "Demo",
            distanceKm = 5m, eventTypeId = 2, eventDate = app.Clock.Now.AddDays(5) };
        await Check("participant cannot modify event", await participant.PutAsJsonAsync(route, replacement), HttpStatusCode.Forbidden);
        await Check("participant cannot delete event", await participant.DeleteAsync(route), HttpStatusCode.Forbidden);
        await Check("other organiser cannot modify event", await other.PutAsJsonAsync(route, replacement), HttpStatusCode.Forbidden);
        await Check("other organiser cannot delete event", await other.DeleteAsync(route), HttpStatusCode.Forbidden);
        await Check("other organiser cannot modify category", await other.PutAsJsonAsync($"/api/categories/{categoryId}", new
        { name = "Denied category", categoryType = "Distance", distanceKm = 5m }), HttpStatusCode.Forbidden);
        await Check("other organiser cannot delete unused category", await other.DeleteAsync($"/api/categories/{ageCategoryId}"), HttpStatusCode.Forbidden);
        await Check("other organiser cannot read enrolments", await other.GetAsync(route + "/enrolments"), HttpStatusCode.Forbidden);
        await Check("other organiser cannot read results", await other.GetAsync(route + "/results"), HttpStatusCode.Forbidden);
        var unchanged = await Check("event unchanged after denied writes", await anonymous.GetAsync(route), HttpStatusCode.OK);
        Assert.Equal(eventJson.GetProperty("name").GetString(), unchanged.GetProperty("name").GetString());
        Assert.Equal(eventJson.GetProperty("eventTypeId").GetInt32(), unchanged.GetProperty("eventTypeId").GetInt32());
        Assert.Equal(eventJson.GetProperty("distanceKm").GetDecimal(), unchanged.GetProperty("distanceKm").GetDecimal());
        Assert.Equal(eventJson.GetProperty("eventDate").GetDateTimeOffset(), unchanged.GetProperty("eventDate").GetDateTimeOffset());
        var categories = unchanged.GetProperty("categories").EnumerateArray().ToArray();
        Assert.Equal(2, categories.Length);
        Assert.Equal("10km Open", categories.Single(x => x.GetProperty("categoryId").GetInt32() == categoryId).GetProperty("name").GetString());
        Assert.Contains(categories, x => x.GetProperty("categoryId").GetInt32() == ageCategoryId);
        var ownedEnrolments = await Check("owner reads enrolments", await owner.GetAsync(route + "/enrolments"), HttpStatusCode.OK);
        Assert.Equal(enrolmentId, Assert.Single(ownedEnrolments.EnumerateArray()).GetProperty("enrolmentId").GetInt32());

        var resultBody = new { finishTime = "00:52:18", finishingPosition = 47 };
        await Check("result before start", await owner.PostAsJsonAsync(resultRoute, resultBody), HttpStatusCode.Conflict);
        // Business time advances without expiring the independent authentication clock.
        app.Clock.Now = app.Clock.Now.AddDays(2);
        await Check("participant cannot record result", await participant.PostAsJsonAsync(resultRoute, resultBody), HttpStatusCode.Forbidden);
        await Check("other organiser cannot record result", await other.PostAsJsonAsync(resultRoute, resultBody), HttpStatusCode.Forbidden);
        var result = await Check("owner records result", await owner.PostAsJsonAsync(resultRoute, resultBody), HttpStatusCode.Created);
        var resultId = result.GetProperty("resultId").GetInt32();
        await Check("duplicate result", await owner.PostAsJsonAsync(resultRoute, resultBody), HttpStatusCode.Conflict);
        var correction = new { finishTime = "00:51:59", finishingPosition = 46 };
        await Check("other organiser cannot correct result", await other.PutAsJsonAsync($"/api/results/{resultId}", correction), HttpStatusCode.Forbidden);
        var beforeCorrection = await Check("result unchanged after denied correction", await owner.GetAsync(route + "/results"), HttpStatusCode.OK);
        Assert.Equal("00:52:18", Assert.Single(beforeCorrection.EnumerateArray()).GetProperty("finishTime").GetString());
        Assert.Equal(47, beforeCorrection[0].GetProperty("finishingPosition").GetInt32());
        await Check("owner corrects result", await owner.PutAsJsonAsync($"/api/results/{resultId}", correction), HttpStatusCode.OK);
        var history = await Check("participant reads corrected history", await participant.GetAsync("/api/users/me/results"), HttpStatusCode.OK);
        Assert.Equal(resultId, Assert.Single(history.EnumerateArray()).GetProperty("resultId").GetInt32());
        Assert.Equal("00:51:59", history[0].GetProperty("finishTime").GetString());
        Assert.Equal(46, history[0].GetProperty("finishingPosition").GetInt32());
        foreach (var client in new[] { owner, other, participant })
        {
            await Check("logout", await client.PostAsync("/api/auth/logout", null), HttpStatusCode.NoContent);
            await Check("protected profile after logout", await client.GetAsync("/api/users/me"), HttpStatusCode.Unauthorized);
        }
    }

    private async Task<JsonElement> Check(string operation, HttpResponseMessage response, HttpStatusCode expected)
    {
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("\"password", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RaceDay.Session", body, StringComparison.OrdinalIgnoreCase);
            // A whitelist keeps profiles, email addresses, cookies, trace IDs and incidental fields out of logs.
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "null" : body);
            output.WriteLine($"{operation}: HTTP {(int)response.StatusCode} {Sanitize(document.RootElement)}");
            Assert.Equal(expected, response.StatusCode);
            return document.RootElement.Clone();
        }
    }

    private static string Sanitize(JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.Array)
            return "[" + string.Join(",", json.EnumerateArray().Select(Sanitize)) + "]";
        if (json.ValueKind != JsonValueKind.Object) return json.GetRawText();
        string[] allowed = ["userId", "role", "eventId", "organiserId", "eventTypeId", "name", "distanceKm", "eventDate",
            "categories", "categoryId", "categoryType", "minAge", "maxAge", "enrolmentId", "enrolmentStatus", "participantId",
            "resultId", "finishTime", "finishingPosition", "status", "title"];
        return "{" + string.Join(",", json.EnumerateObject().Where(p => allowed.Contains(p.Name))
            .Select(p => JsonSerializer.Serialize(p.Name) + ":" + Sanitize(p.Value))) + "}";
    }
}
