using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public sealed class OrganiserController(RaceDayApiClient api) : Controller
{
    private bool Allowed() => HttpContext.Session.GetInt32("UserId") is not null &&
        string.Equals(HttpContext.Session.GetString("Role"), "Organiser", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        return View(await api.ReadAsync<List<ApiEvent>>("api/events") ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Events()
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        return View(await api.ReadAsync<List<ApiEvent>>("api/events") ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> CreateEvent()
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        ViewBag.EventTypes = await api.ReadAsync<List<ApiEventType>>("api/event-types") ?? [];
        return View(new EventFormVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormVm model)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        if (!ModelState.IsValid) { ViewBag.EventTypes = await api.ReadAsync<List<ApiEventType>>("api/event-types") ?? []; return View(model); }
        using var response = await api.SendAsync(HttpMethod.Post, "api/events", new { model.Name, model.Description, EventDate = model.EventDate, model.Location, model.DistanceKm, model.EventTypeId });
        if (!response.IsSuccessStatusCode) { ModelState.AddModelError("", await RaceDayApiClient.ErrorAsync(response)); ViewBag.EventTypes = await api.ReadAsync<List<ApiEventType>>("api/event-types") ?? []; return View(model); }
        var created = await RaceDayApiClient.ReadAsync<ApiEvent>(response);
        if (created is not null && model.Banner is not null)
        {
            using var upload = await api.UploadAsync($"api/events/{created.EventId}/banner", model.Banner);
            if (!upload.IsSuccessStatusCode) TempData["Error"] = await RaceDayApiClient.ErrorAsync(upload);
        }
        TempData["Success"] = "Event created.";
        return RedirectToAction(nameof(Events), created is null ? null : new { id = created.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        var item = await api.ReadAsync<ApiEvent>($"api/events/{id}");
        if (item is null) return NotFound();
        ViewBag.EventTypes = await api.ReadAsync<List<ApiEventType>>("api/event-types") ?? [];
        return View(new EventFormVm { EventId = item.EventId, Name = item.Name, Description = item.Description, EventDate = item.EventDate.ToLocalTime(), Location = item.Location, DistanceKm = item.DistanceKm, EventTypeId = item.EventTypeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormVm model)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        if (!ModelState.IsValid) { ViewBag.EventTypes = await api.ReadAsync<List<ApiEventType>>("api/event-types") ?? []; return View(model); }
        using var response = await api.SendAsync(HttpMethod.Put, $"api/events/{model.EventId}", new { model.Name, model.Description, EventDate = model.EventDate, model.Location, model.DistanceKm, model.EventTypeId });
        if (!response.IsSuccessStatusCode) { ModelState.AddModelError("", await RaceDayApiClient.ErrorAsync(response)); ViewBag.EventTypes = await api.ReadAsync<List<ApiEventType>>("api/event-types") ?? []; return View(model); }
        if (model.Banner is not null)
        {
            using var upload = await api.UploadAsync($"api/events/{model.EventId}/banner", model.Banner);
            if (!upload.IsSuccessStatusCode) TempData["Error"] = await RaceDayApiClient.ErrorAsync(upload);
        }
        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        using var response = await api.SendAsync(HttpMethod.Delete, $"api/events/{id}");
        if (!response.IsSuccessStatusCode) TempData["Error"] = await RaceDayApiClient.ErrorAsync(response);
        else TempData["Success"] = "Event deleted.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> Enrolments(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        ViewBag.Event = await api.ReadAsync<ApiEvent>($"api/events/{id}");
        return View(await api.ReadAsync<List<ApiEnrolment>>($"api/events/{id}/enrolments") ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Categories(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        ViewBag.Event = await api.ReadAsync<ApiEvent>($"api/events/{id}");
        return View(await api.ReadAsync<List<ApiCategory>>($"api/events/{id}/categories") ?? []);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryFormVm model)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        using var response = await api.SendAsync(HttpMethod.Post, $"api/events/{model.EventId}/categories",
            new { model.CategoryType, model.Name, model.MinAge, model.MaxAge, model.DistanceKm });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Category added." : await RaceDayApiClient.ErrorAsync(response);
        return RedirectToAction(nameof(Categories), new { id = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id, int eventId)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        var categories = await api.ReadAsync<List<ApiCategory>>($"api/events/{eventId}/categories") ?? [];
        var item = categories.FirstOrDefault(x => x.CategoryId == id);
        return item is null ? NotFound() : View(new CategoryFormVm { EventId = eventId, CategoryId = item.CategoryId, CategoryType = item.CategoryType, Name = item.Name, MinAge = item.MinAge, MaxAge = item.MaxAge, DistanceKm = item.DistanceKm });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryFormVm model)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        using var response = await api.SendAsync(HttpMethod.Put, $"api/categories/{model.CategoryId}",
            new { model.CategoryType, model.Name, model.MinAge, model.MaxAge, model.DistanceKm });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Category updated." : await RaceDayApiClient.ErrorAsync(response);
        return RedirectToAction(nameof(Categories), new { id = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        using var response = await api.SendAsync(HttpMethod.Delete, $"api/categories/{id}");
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Category deleted." : await RaceDayApiClient.ErrorAsync(response);
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Results(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        ViewBag.Event = await api.ReadAsync<ApiEvent>($"api/events/{id}");
        ViewBag.Enrolments = await api.ReadAsync<List<ApiEnrolment>>($"api/events/{id}/enrolments") ?? [];
        return View(await api.ReadAsync<List<ApiResult>>($"api/events/{id}/results") ?? []);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddResult(ResultFormVm model, int eventId)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        using var response = await api.SendAsync(HttpMethod.Post, $"api/enrolments/{model.EnrolmentId}/result", new { model.FinishTime, model.FinishingPosition });
        if (!response.IsSuccessStatusCode) TempData["Error"] = await RaceDayApiClient.ErrorAsync(response);
        else TempData["Success"] = "Result recorded.";
        return RedirectToAction(nameof(Results), new { id = eventId });
    }
}
