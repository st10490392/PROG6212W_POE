using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public sealed class ParticipantController(RaceDayApiClient api) : Controller
{
    private bool Allowed() => HttpContext.Session.GetInt32("UserId") is not null &&
        string.Equals(HttpContext.Session.GetString("Role"), "Participant", StringComparison.OrdinalIgnoreCase);

    [HttpGet]
    public async Task<IActionResult> Index() => !Allowed() ? RedirectToAction("Login", "Auth") : View(await api.ReadAsync<List<ApiEvent>>("api/events") ?? []);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        using var response = await api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/enrolments", new { categoryId });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "You are enrolled." : await RaceDayApiClient.ErrorAsync(response);
        return RedirectToAction("Details", "Events", new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> MyEvents() => !Allowed() ? RedirectToAction("Login", "Auth") : View(await api.ReadAsync<List<ApiEnrolment>>("api/users/me/enrolments") ?? []);

    [HttpGet]
    public async Task<IActionResult> MyResults() => !Allowed() ? RedirectToAction("Login", "Auth") : View(await api.ReadAsync<List<ApiResult>>("api/users/me/results") ?? []);

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        var profile = await api.ReadAsync<ApiProfile>("api/users/me");
        return profile is null ? RedirectToAction("Login", "Auth") : View(new ProfileVm { UserId = profile.UserId, FirstName = profile.FirstName, LastName = profile.LastName, Email = profile.Email, Role = profile.Role, Phone = profile.Phone, DateOfBirth = profile.DateOfBirth, ProfilePictureUrl = profile.ProfilePictureUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm model)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        if (!ModelState.IsValid) return View(model);
        using var response = await api.SendAsync(HttpMethod.Put, "api/users/me", new { model.FirstName, model.LastName, model.Phone, model.DateOfBirth, model.ProfilePictureUrl });
        if (!response.IsSuccessStatusCode) ModelState.AddModelError("", await RaceDayApiClient.ErrorAsync(response));
        else TempData["Success"] = "Profile updated.";
        if (model.ProfilePicture is not null)
        {
            using var upload = await api.UploadAsync("api/users/me/profile-picture", model.ProfilePicture);
            if (!upload.IsSuccessStatusCode) TempData["Error"] = await RaceDayApiClient.ErrorAsync(upload);
            else TempData["Success"] = "Profile updated.";
        }
        return View(model);
    }
}
