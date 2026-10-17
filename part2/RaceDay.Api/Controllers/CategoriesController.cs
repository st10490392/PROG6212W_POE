using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController(RaceDayDbContext db, AccessService access) : ControllerBase
{
    [HttpGet("api/events/{eventId:int}/categories")]
    public async Task<IActionResult> GetAll(int eventId)
    {
        if (!await db.Events.AnyAsync(x => x.EventId == eventId)) throw new ApiException(404, "Event not found.");
        return Ok((await db.Categories.AsNoTracking().Where(x => x.EventId == eventId)
            .OrderBy(x => x.CategoryId).ToListAsync()).Select(Responses.Category));
    }

    [HttpPost("api/events/{eventId:int}/categories"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        await access.OwnedEvent(eventId);
        var category = new Category { EventId = eventId };
        Apply(category, request);
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return Created($"/api/events/{eventId}/categories", Responses.Category(category));
    }

    [HttpPut("api/categories/{categoryId:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Update(int categoryId, CategoryRequest request)
    {
        var category = await FindOwned(categoryId);
        Apply(category, request);
        await db.SaveChangesAsync();
        return Ok(Responses.Category(category));
    }

    [HttpDelete("api/categories/{categoryId:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Delete(int categoryId)
    {
        var category = await FindOwned(categoryId);
        if (await db.Enrolments.AnyAsync(x => x.CategoryId == categoryId))
            throw new ApiException(409, "Category is already used by an enrolment.");
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Category> FindOwned(int id)
    {
        var category = await db.Categories.FindAsync(id) ?? throw new ApiException(404, "Category not found.");
        await access.OwnedEvent(category.EventId);
        return category;
    }
    private static void Apply(Category category, CategoryRequest request)
    {
        category.CategoryType = request.CategoryType; category.Name = request.Name.Trim();
        category.MinAge = request.MinAge; category.MaxAge = request.MaxAge; category.DistanceKm = request.DistanceKm;
    }
}
