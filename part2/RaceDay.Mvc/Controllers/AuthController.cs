using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public sealed class AuthController(RaceDayApiClient api) : Controller
{
    [HttpGet] public IActionResult Login() => View();
    [HttpGet] public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid) return View(model);
        using var response = await api.SendAsync(HttpMethod.Post, "api/auth/login", new { model.Email, model.Password });
        if (!response.IsSuccessStatusCode) { ModelState.AddModelError("", await RaceDayApiClient.ErrorAsync(response)); return View(model); }
        var profile = await RaceDayApiClient.ReadAsync<ProfileVm>(response);
        if (profile is null) { ModelState.AddModelError("", "The API returned an unreadable login response."); return View(model); }
        var cookie = RaceDayApiClient.SessionCookie(response);
        if (!string.IsNullOrWhiteSpace(cookie)) HttpContext.Session.SetString("ApiSessionCookie", cookie);
        HttpContext.Session.SetInt32("UserId", profile.UserId);
        HttpContext.Session.SetString("Role", profile.Role);
        HttpContext.Session.SetString("DisplayName", $"{profile.FirstName} {profile.LastName}");
        return RedirectToAction("Index", profile.Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase) ? "Organiser" : "Participant");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (!ModelState.IsValid) return View(model);
        using var response = await api.SendAsync(HttpMethod.Post, "api/auth/register", model);
        if (!response.IsSuccessStatusCode) { ModelState.AddModelError("", await RaceDayApiClient.ErrorAsync(response)); return View(model); }
        TempData["Success"] = "Registration complete. Sign in to continue.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        using var response = await api.SendAsync(HttpMethod.Post, "api/auth/logout");
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
