using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

public sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2030, 6, 1, 8, 0, 0, TimeSpan.Zero);
    public bool Fail { get; set; }
    public override DateTimeOffset GetUtcNow() => Fail ? throw new InvalidOperationException("Test-only sensitive diagnostic") : Now;
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:;Foreign Keys=True");
    private readonly string? sqlServer = Environment.GetEnvironmentVariable("RACEDAY_TEST_SQLSERVER");
    private bool initialized;
    private bool cleaned;
    public TestClock Clock { get; } = new();
    public TestClock SessionClock { get; } = new() { Now = DateTimeOffset.UtcNow };
    public bool UsesSqlServer => !string.IsNullOrWhiteSpace(sqlServer);
    public string DatabaseName { get; } = "RaceDayTests_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<RaceDayDbContext>();
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RaceDayDbContext>>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            // Event tests advance business time by days independently of session expiry.
            services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,
                options => options.TimeProvider = SessionClock);
            if (UsesSqlServer)
            {
                var settings = new SqlConnectionStringBuilder(sqlServer!) { InitialCatalog = DatabaseName };
                services.AddDbContext<RaceDayDbContext>(options => options.UseSqlServer(settings.ConnectionString));
            }
            else
            {
                connection.Open();
                services.AddDbContext<RaceDayDbContext>(options => options.UseSqlite(connection));
            }
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        if (UsesSqlServer) db.Database.Migrate(); else db.Database.EnsureCreated();
        initialized = true;
        return host;
    }

    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });

    public async Task<HttpClient> Actor(string role = "Organiser")
    {
        var client = Browser();
        var email = Guid.NewGuid().ToString("N") + "@raceday.test";
        var created = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Lerato", lastName = "Mokoena", email, password = "Coursework-Password-42", role
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Coursework-Password-42" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    public object EventBody(int type = 1, DateTimeOffset? date = null) => new
    {
        name = "Polokwane Community Run", description = "Community road event", location = "Polokwane, Limpopo",
        distanceKm = 10m, eventTypeId = type, eventDate = date ?? Clock.Now.AddDays(1)
    };
    public async Task<int> Event(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await Json(response)).GetProperty("eventId").GetInt32();
    }
    public static object CategoryBody => new { categoryType = "Distance", name = "10km Open", distanceKm = 10m };
    public async Task<int> Category(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await Json(response)).GetProperty("categoryId").GetInt32();
    }
    public async Task<int> Enrol(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await Json(response)).GetProperty("enrolmentId").GetInt32();
    }
    public async Task WithDb(Func<RaceDayDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<RaceDayDbContext>());
    }
    public static async Task<JsonElement> Json(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>());

    public override async ValueTask DisposeAsync()
    {
        if (initialized && !cleaned && UsesSqlServer)
        {
            cleaned = true;
            // Only the randomly named database created by this test instance is ever removed.
            await WithDb(async db =>
            {
                Assert.Equal(DatabaseName, db.Database.GetDbConnection().Database);
                Assert.StartsWith("RaceDayTests_", DatabaseName);
                await db.Database.EnsureDeletedAsync();
            });
        }
        await base.DisposeAsync();
        await connection.DisposeAsync();
    }
}
