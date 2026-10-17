using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using RaceDay.Api.Security;

namespace RaceDay.Tests;

public class SessionStoreTests
{
    private static AuthenticationTicket Ticket(DateTimeOffset expires) => new(
        new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Role, "Participant")], "Cookies")),
        new AuthenticationProperties { ExpiresUtc = expires }, "Cookies");

    [Fact]
    public async Task Session_keys_are_unique_and_revocation_preserves_other_browser_sessions()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new ServerTicketStore(cache);
        var ticket = Ticket(DateTimeOffset.UtcNow.AddMinutes(30));
        var first = await store.StoreAsync(ticket);
        var second = await store.StoreAsync(ticket);
        Assert.Equal(64, first.Length); // 32 random bytes encoded as hex.
        Assert.NotEqual(first, second);
        Assert.Equal("42", (await store.RetrieveAsync(first))!.Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await store.RemoveAsync(first);
        Assert.Null(await store.RetrieveAsync(first));
        Assert.NotNull(await store.RetrieveAsync(second));
    }

    [Fact]
    public async Task Expired_server_ticket_cannot_be_retrieved()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new ServerTicketStore(cache);
        var key = await store.StoreAsync(Ticket(DateTimeOffset.UtcNow.AddMinutes(-1)));
        Assert.Null(await store.RetrieveAsync(key));
    }
}
