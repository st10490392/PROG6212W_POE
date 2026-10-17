using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class ProfileTests
{
    [Fact]
    public async Task Profile_update_is_persisted_and_cannot_change_role_or_identity()
    {
        await using var app = new ApiFactory();
        using var participant = await app.Actor("Participant");
        var before = await ApiFactory.Json(await participant.GetAsync("/api/users/me"));
        var response = await participant.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Ayanda", lastName = "Dlamini", phone = "0845550103", dateOfBirth = "2001-06-20",
            profilePictureUrl = "https://example.com/photo.jpg", role = "Organiser", userId = 999
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await ApiFactory.Json(await participant.GetAsync("/api/users/me"));
        Assert.Equal("Ayanda", profile.GetProperty("firstName").GetString());
        Assert.Equal("Participant", profile.GetProperty("role").GetString());
        Assert.Equal(before.GetProperty("userId").GetInt32(), profile.GetProperty("userId").GetInt32());
        Assert.Equal("2001-06-20", profile.GetProperty("dateOfBirth").GetString());
        Assert.False(profile.TryGetProperty("passwordHash", out _));
    }

    [Theory]
    [InlineData("", "https://example.com/photo.jpg", "2001-06-20")]
    [InlineData("Ayanda", "invalid-url", "2001-06-20")]
    [InlineData("Ayanda", "https://example.com/photo.jpg", "9999-12-31")]
    public async Task Invalid_profile_is_rejected(string firstName, string profilePictureUrl, string dateOfBirth)
    {
        await using var app = new ApiFactory();
        using var participant = await app.Actor("Participant");
        var response = await participant.PutAsJsonAsync("/api/users/me", new { firstName, lastName = "Dlamini", profilePictureUrl, dateOfBirth });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
