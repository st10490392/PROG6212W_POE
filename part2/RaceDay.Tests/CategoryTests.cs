using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class CategoryTests
{
    [Fact]
    public async Task Category_CRUD_is_public_for_reads_and_restricted_to_event_owner_for_writes()
    {
        await using var app = new ApiFactory();
        using var owner = await app.Actor();
        using var other = await app.Actor();
        using var visitor = app.Browser();
        var eventId = await app.Event(owner);
        var categoryId = await app.Category(owner, eventId);
        Assert.Equal(1, (await ApiFactory.Json(await visitor.GetAsync($"/api/events/{eventId}/categories"))).GetArrayLength());
        Assert.Equal(1, (await ApiFactory.Json(await visitor.GetAsync($"/api/events/{eventId}"))).GetProperty("categories").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync($"/api/events/{eventId}/categories", ApiFactory.CategoryBody)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/categories/{categoryId}", ApiFactory.CategoryBody)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/categories/{categoryId}", new
        { categoryType = "Age", name = "Senior", minAge = 20, maxAge = 39 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/events/999/categories")).StatusCode);
    }

    [Theory]
    [InlineData("Age", -1, 39, null)]
    [InlineData("Age", 40, 20, null)]
    [InlineData("Age", null, null, null)]
    [InlineData("Distance", null, null, 0)]
    [InlineData("Distance", 20, 39, 10)]
    [InlineData("Unknown", null, null, 10)]
    public async Task Invalid_categories_return_400(string categoryType, int? minAge, int? maxAge, int? distanceKm)
    {
        await using var app = new ApiFactory();
        using var owner = await app.Actor();
        var eventId = await app.Event(owner);
        var response = await owner.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        { categoryType, name = "Invalid", minAge, maxAge, distanceKm });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
