using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController, Authorize, Route("api/users/me")]
public class UsersController(RaceDayDbContext db, AccessService access) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile() => Ok(Responses.Profile(
        await db.Users.FindAsync(access.UserId) ?? throw new ApiException(401, "Account no longer exists.")));

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(ProfileRequest request)
    {
        var user = await db.Users.FindAsync(access.UserId) ?? throw new ApiException(401, "Account no longer exists.");
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Phone = request.Phone;
        user.DateOfBirth = request.DateOfBirth;
        user.ProfilePictureUrl = request.ProfilePictureUrl;
        await db.SaveChangesAsync();
        return Ok(Responses.Profile(user));
    }
}
