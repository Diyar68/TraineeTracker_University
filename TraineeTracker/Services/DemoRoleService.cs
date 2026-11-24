// Services/DemoRoleService.cs
using System.Security.Claims;

namespace TraineeTracker.Services;

/// <summary>
/// Liest die aktuelle Rolle aus den Claims des angemeldeten Nutzers.
/// (Name ist historisch — es gibt keine Demo-Rollenquelle mehr, siehe RequireDemoRoleAttribute.)
/// </summary>
public class DemoRoleService : IDemoRoleService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DemoRoleService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CurrentRole =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role)
        ?? GetRoleCookie(_httpContextAccessor.HttpContext);

    // Cokkies für die Rollenverwaltung
    private static string? GetRoleCookie(HttpContext? context)
    {
        var roleCookie = context?.Request?.Cookies["role"];
        return roleCookie is "mentor" or "admin" or "trainee"
            ? roleCookie
            : null;
    }

    public bool IsMentorOrAdmin() =>
        string.Equals(CurrentRole, AppConstants.RoleValues.Mentor, StringComparison.OrdinalIgnoreCase)
        || string.Equals(CurrentRole, AppConstants.RoleValues.Admin, StringComparison.OrdinalIgnoreCase);
}
