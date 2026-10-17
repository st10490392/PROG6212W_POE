using System.Net;

namespace RaceDay.Tests;

public class ErrorTests
{
    [Fact]
    public async Task Unexpected_exception_returns_safe_500_problem_without_diagnostics()
    {
        await using var app = new ApiFactory();
        using var visitor = app.Browser();
        app.Clock.Fail = true;
        var response = await visitor.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("sensitive", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", body);
        var problem = await ApiFactory.Json(response);
        Assert.Equal("An unexpected server error occurred.", problem.GetProperty("title").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }
}
