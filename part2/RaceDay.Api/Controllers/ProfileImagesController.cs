using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController, Authorize, Route("api/users/me/profile-picture")]
public sealed class ProfileImagesController(AccessService access, BlobStorageService blobs, RaceDayDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var user = await db.Users.FindAsync(access.UserId) ?? throw new ApiException(401, "Account no longer exists.");
        user.ProfilePictureUrl = await blobs.UploadAsync(file, "profile-pictures");
        await db.SaveChangesAsync();
        return Ok(new { imageUrl = user.ProfilePictureUrl });
    }
}
