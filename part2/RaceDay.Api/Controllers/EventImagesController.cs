using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/events")]
public sealed class EventImagesController(AccessService access, BlobStorageService blobs, RaceDayDbContext db) : ControllerBase
{
    [HttpPost("{eventId:int}/banner"), Authorize(Roles = Models.Roles.Organiser)]
    public async Task<IActionResult> UploadBanner(int eventId, IFormFile file)
    {
        var eventItem = await access.OwnedEvent(eventId);
        eventItem.BannerImageUrl = await blobs.UploadAsync(file, "event-banners");
        await db.SaveChangesAsync();
        return Ok(new { imageUrl = eventItem.BannerImageUrl });
    }
}
