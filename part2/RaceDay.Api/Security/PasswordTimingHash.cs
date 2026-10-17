using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RaceDay.Api.Models;

namespace RaceDay.Api.Security;

// Compute once, then verify once per failed login regardless of whether the email exists.
public sealed class PasswordTimingHash(IOptions<PasswordHasherOptions> options)
{
    public string Hash { get; } = new PasswordHasher<User>(options)
        .HashPassword(new User(), "Unknown-user-timing-placeholder");
}
