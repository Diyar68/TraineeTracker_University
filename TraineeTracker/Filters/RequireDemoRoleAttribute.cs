// Filters/RequireDemoRoleAttribute.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TraineeTracker.Filters;

/// <summary>
/// Rollenschutz auf Basis der echten Anmeldung (signiertes Auth-Cookie + Claims).
/// Der Name ist historisch — ursprünglich prüfte dieser Filter nur ein frei
/// beschreibbares "role"-Cookie. Er ist jetzt an das ASP.NET-Core-Cookie-Auth
/// angebunden, sodass Nutzer sich nicht mehr per Cookie in fremde Rollen
/// versetzen können.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireDemoRoleAttribute : ActionFilterAttribute
{
    private readonly string[] _allowedRoles;

    public RequireDemoRoleAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        // Niemand angemeldet -> zur Login-Seite.
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var role = user.FindFirstValue(ClaimTypes.Role);
        var isAllowed = _allowedRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

        // Angemeldet, aber falsche Rolle -> zur eigenen Startseite dieser Rolle,
        // statt einer Fehlerseite, damit der Nutzungsfluss nicht unterbrochen wird.
        if (!isAllowed)
        {
            context.Result = new RedirectResult(LandingUrlFor(role));
            return;
        }

        base.OnActionExecuting(context);
    }

    public static string LandingUrlFor(string? role) => role?.Trim().ToLowerInvariant() switch
    {
        AppConstants.RoleValues.Admin => "/Admin",
        AppConstants.RoleValues.Mentor => "/TraineeDashboard",
        AppConstants.RoleValues.Trainee => "/TraineeDashboard",
        _ => "/Account/Login",
    };
}
