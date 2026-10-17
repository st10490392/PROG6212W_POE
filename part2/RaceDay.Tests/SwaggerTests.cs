using System.Net;
using System.Text.Json;

namespace RaceDay.Tests;

public class SwaggerTests
{
    [Fact]
    public async Task Swagger_exposes_all_24_planned_operations_and_can_share_browser_session()
    {
        await using var app = new ApiFactory();
        using var browser = await app.Actor();
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/swagger/index.html")).StatusCode);
        var response = await browser.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await ApiFactory.Json(response);
        var paths = document.GetProperty("paths");
        var operations = paths.EnumerateObject().Sum(x => x.Value.EnumerateObject().Count());
        Assert.Equal(24, operations);
        Assert.True(paths.TryGetProperty("/api/auth/login", out _));
        Assert.True(paths.TryGetProperty("/api/users/me/results", out _));
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/users/me")).StatusCode);
    }
}
