using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class AuthTests
{
    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Registration_hashes_password_and_login_creates_revocable_session(string role)
    {
        await using var app = new ApiFactory();
        using var client = app.Browser();
        const string password = "Coursework-Password-42";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        { firstName = "Ayanda", lastName = "Dlamini", email = "AYANDA@RACEDAY.TEST", password, role });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.DoesNotContain("password", await register.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        await app.WithDb(async db =>
        {
            var user = await db.Users.SingleAsync();
            Assert.Equal("ayanda@raceday.test", user.Email);
            Assert.NotEqual(password, user.PasswordHash);
            Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, password));
        });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "ayanda@raceday.test", password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), x => x.StartsWith("RaceDay.Session="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        using var replay = app.Browser();
        replay.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task Duplicate_email_is_case_insensitive_and_returns_conflict()
    {
        await using var app = new ApiFactory();
        using var client = app.Browser();
        foreach (var email in new[] { "person@raceday.test", "PERSON@RACEDAY.TEST" })
        {
            var response = await client.PostAsJsonAsync("/api/auth/register", new
            { firstName = "A", lastName = "B", email, password = "Coursework-Password-42", role = "Participant" });
            Assert.Equal(email.StartsWith("person") ? HttpStatusCode.Created : HttpStatusCode.Conflict, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("not-an-email", "Coursework-Password-42", "Participant")]
    [InlineData("person@raceday.test", "short", "Participant")]
    [InlineData("person@raceday.test", "Coursework-Password-42", "Admin")]
    public async Task Invalid_registration_returns_validation_problem(string email, string password, string role)
    {
        await using var app = new ApiFactory();
        using var client = app.Browser();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { firstName = "A", lastName = "B", email, password, role });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ApiFactory.Json(response)).TryGetProperty("errors", out _));
    }

    [Theory]
    [InlineData("unknown@raceday.test")]
    [InlineData("known@raceday.test")]
    public async Task Invalid_credentials_use_same_401_message(string email)
    {
        await using var app = new ApiFactory();
        using var client = app.Browser();
        await client.PostAsJsonAsync("/api/auth/register", new
        { firstName = "A", lastName = "B", email = "known@raceday.test", password = "Coursework-Password-42", role = "Participant" });
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong-password-42" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Invalid email or password.", (await ApiFactory.Json(response)).GetProperty("title").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Re_login_revokes_old_cookie_and_cross_origin_writes_are_rejected()
    {
        await using var app = new ApiFactory();
        using var client = app.Browser();
        const string email = "repeat@raceday.test";
        await client.PostAsJsonAsync("/api/auth/register", new
        { firstName = "A", lastName = "B", email, password = "Coursework-Password-42", role = "Participant" });
        var first = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Coursework-Password-42" });
        var oldCookie = Assert.Single(first.Headers.GetValues("Set-Cookie")).Split(';')[0];
        await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Coursework-Password-42" });
        using var replay = app.Browser();
        replay.DefaultRequestHeaders.Add("Cookie", oldCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/users/me")).StatusCode);
        client.DefaultRequestHeaders.Add("Origin", "https://malicious.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
    }
}
