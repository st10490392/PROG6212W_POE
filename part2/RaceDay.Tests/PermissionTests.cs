using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class PermissionTests
{
    public static TheoryData<string, string> ProtectedRoutes => new()
    {
        { "GET", "/api/users/me" }, { "PUT", "/api/users/me" }, { "POST", "/api/auth/logout" },
        { "POST", "/api/events" }, { "PUT", "/api/events/1" }, { "DELETE", "/api/events/1" },
        { "POST", "/api/events/1/categories" }, { "PUT", "/api/categories/1" }, { "DELETE", "/api/categories/1" },
        { "POST", "/api/events/1/enrolments" }, { "GET", "/api/users/me/enrolments" },
        { "GET", "/api/events/1/enrolments" }, { "PUT", "/api/enrolments/1" }, { "DELETE", "/api/enrolments/1" },
        { "POST", "/api/enrolments/1/result" }, { "PUT", "/api/results/1" },
        { "GET", "/api/users/me/results" }, { "GET", "/api/events/1/results" }
    };

    [Theory, MemberData(nameof(ProtectedRoutes))]
    public async Task Every_protected_route_returns_401_without_redirect(string method, string route)
    {
        await using var app = new ApiFactory();
        using var client = app.Browser();
        using var request = new HttpRequestMessage(new HttpMethod(method), route) { Content = JsonContent.Create(new { }) };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/1")]
    [InlineData("DELETE", "/api/events/1")]
    [InlineData("POST", "/api/events/1/categories")]
    [InlineData("PUT", "/api/categories/1")]
    [InlineData("DELETE", "/api/categories/1")]
    [InlineData("GET", "/api/events/1/enrolments")]
    [InlineData("POST", "/api/enrolments/1/result")]
    [InlineData("PUT", "/api/results/1")]
    [InlineData("GET", "/api/events/1/results")]
    public async Task Participants_cannot_use_organiser_endpoints(string method, string route)
    {
        await using var app = new ApiFactory();
        using var participant = await app.Actor("Participant");
        using var request = new HttpRequestMessage(new HttpMethod(method), route) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.SendAsync(request)).StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/events/1/enrolments")]
    [InlineData("GET", "/api/users/me/enrolments")]
    [InlineData("PUT", "/api/enrolments/1")]
    [InlineData("DELETE", "/api/enrolments/1")]
    [InlineData("GET", "/api/users/me/results")]
    public async Task Organisers_cannot_use_participant_endpoints(string method, string route)
    {
        await using var app = new ApiFactory();
        using var organiser = await app.Actor();
        using var request = new HttpRequestMessage(new HttpMethod(method), route) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.SendAsync(request)).StatusCode);
    }
}
