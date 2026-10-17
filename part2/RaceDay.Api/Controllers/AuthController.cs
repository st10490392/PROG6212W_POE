using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(RaceDayDbContext db, IPasswordHasher<User> hasher, PasswordTimingHash timing) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email)) throw new ApiException(409, "Email is already registered.");
        var user = new User { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
            Email = email, Role = request.Role, Phone = request.Phone, DateOfBirth = request.DateOfBirth };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return StatusCode(201, Responses.Profile(user));
    }

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
        // Do equivalent password work for unknown users; never reveal whether an email exists.
        var candidate = user ?? new User();
        var hash = user?.PasswordHash ?? timing.Hash;
        var verification = hasher.VerifyHashedPassword(candidate, hash, request.Password);
        if (user is null || verification == PasswordVerificationResult.Failed)
            throw new ApiException(401, "Invalid email or password.");
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync();
        }
        // Re-login revokes the old browser session before issuing a fresh session key.
        await HttpContext.SignOutAsync();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Role, user.Role), new Claim(ClaimTypes.Name, user.Email)
        ], CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(principal, new AuthenticationProperties
        {
            IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30), AllowRefresh = false
        });
        return Ok(Responses.Profile(user));
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }
}
