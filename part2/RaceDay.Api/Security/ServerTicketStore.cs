using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace RaceDay.Api.Security;

// Only a random lookup key travels in the protected cookie. User ID/role stay on the server.
// Suitable for a single coursework API instance; restarting it invalidates every session.
public sealed class ServerTicketStore(IMemoryCache cache) : ITicketStore
{
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        Save(key, ticket);
        return Task.FromResult(key);
    }
    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        Save(key, ticket);
        return Task.CompletedTask;
    }
    public Task<AuthenticationTicket?> RetrieveAsync(string key) =>
        Task.FromResult(cache.Get<AuthenticationTicket>("session:" + key));
    public Task RemoveAsync(string key)
    {
        cache.Remove("session:" + key);
        return Task.CompletedTask;
    }
    private void Save(string key, AuthenticationTicket ticket) =>
        cache.Set("session:" + key, ticket, ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddMinutes(30));
}
