using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class EventsController(RaceDayApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? eventTypeId, DateOnly? date)
    {
        var query = new List<string>();
        if (eventTypeId is not null) query.Add($"eventTypeId={eventTypeId}");
        if (date is not null) query.Add($"date={date:yyyy-MM-dd}");
        var suffix = query.Count == 0 ? "" : $"?{string.Join("&", query)}";
        var events = await api.ReadAsync<List<ApiEvent>>($"api/events{suffix}") ?? [];
        ViewBag.EventTypes = await api.ReadAsync<List<ApiEventType>>("api/event-types") ?? [];
        ViewBag.EventTypeId = eventTypeId;
        ViewBag.Date = date;
        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await api.ReadAsync<ApiEvent>($"api/events/{id}");
        return item is null ? NotFound() : View(item);
    }
}
