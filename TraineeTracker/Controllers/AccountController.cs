using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Filters;
using TraineeTracker.Services;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Controllers;

public class AccountController : Controller
{
    private readonly UserRepository _userRepository;

    public AccountController(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return Redirect(RequireDemoRoleAttribute.LandingUrlFor(role));
        }

        ViewData["Title"] = "Anmelden";
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        ViewData["Title"] = "Anmelden";

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var user = _userRepository.GetByEmail(vm.Email);

        if (user == null || !PasswordHasher.Verify(vm.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "E-Mail-Adresse oder Passwort ist falsch.");
            return View(vm);
        }

        if (user.IsClosed)
        {
            ModelState.AddModelError(string.Empty, "Dieses Konto ist deaktiviert. Bitte wende dich an die Administration.");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role),
            new("sandbox", ((DemoSessionStore.Session)HttpContext.Items[DemoSessionStore.ItemName]!).Id),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        // Rein kosmetisches Cookie für die Anzeige/Umschaltung in den Views.
        // Für Berechtigungen zählt ausschließlich das signierte Auth-Cookie oben.
        Response.Cookies.Append(AppConstants.Cookies.Role, user.Role.ToLowerInvariant(), new CookieOptions { MaxAge = DemoSessionStore.Lifetime });
        Response.Cookies.Delete(AppConstants.Cookies.MentorTrainee);

        return Redirect(RequireDemoRoleAttribute.LandingUrlFor(user.Role));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete(AppConstants.Cookies.Role);
        Response.Cookies.Delete(AppConstants.Cookies.MentorTrainee);
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDemo()
    {
        var session = (DemoSessionStore.Session)HttpContext.Items[DemoSessionStore.ItemName]!;
        session.Expires = DateTimeOffset.UtcNow;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete(DemoSessionStore.CookieName);
        Response.Cookies.Delete(AppConstants.Cookies.Role);
        Response.Cookies.Delete(AppConstants.Cookies.MentorTrainee);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Error() => Problem("The request could not be completed.", statusCode: 500);

}
