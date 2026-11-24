// Helpers/ClaimsPrincipalExtensions.cs
using System.Security.Claims;

namespace TraineeTracker.Helpers;

/// <summary>
/// Kleine Helfer, um die beim Login gesetzten Claims (Id, Name, Rolle) auszulesen,
/// ohne dass jeder Controller einzeln mit ClaimTypes hantieren muss.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string GetFullName(this ClaimsPrincipal user) =>
        $"{user.FindFirstValue(ClaimTypes.GivenName)} {user.FindFirstValue(ClaimTypes.Surname)}".Trim();

    public static string? GetRole(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role);
}
