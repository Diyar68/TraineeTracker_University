using Microsoft.AspNetCore.Mvc;

// Verwaltet die Aktionen die vom Mentor ausführbar sind. 
// Da der Mentor noch im TraineeDashboard mit verankert ist, ist dieser Controller kleiner

namespace TraineeTracker.Controllers;

[TraineeTracker.Filters.RequireDemoRole(AppConstants.RoleValues.Mentor, AppConstants.RoleValues.Admin)]
public class MentorController : Controller
{
    // Setzt das Server-seitige Cookie für die Trainee-Auswahl des Mentors und leitet zur Dashboard weiter.
   
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SelectTrainee(string traineeId)
    {
        if (string.IsNullOrWhiteSpace(traineeId))
            return BadRequest("TraineeId erforderlich");

        var ownId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var assignments = HttpContext.RequestServices.GetRequiredService<TraineeTracker.Data.Repositories.MentorAssignmentRepository>();
        if (role != AppConstants.Roles.Admin && !assignments.GetTraineeIdsForMentor(ownId!).Contains(traineeId))
            return StatusCode(403);
        var selected = HttpContext.RequestServices.GetRequiredService<TraineeTracker.Data.Repositories.UserRepository>().GetById(traineeId);
        if (selected == null || selected.IsClosed || selected.Role != AppConstants.Roles.Trainee) return StatusCode(403);
        Response.Cookies.Append(
            AppConstants.Cookies.MentorTrainee,
            traineeId,
            new CookieOptions 
            { 
                MaxAge = TimeSpan.FromHours(1),
                HttpOnly = true,
                Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
                SameSite = SameSiteMode.Lax
            }
        );
        return RedirectToAction("Index", "TraineeDashboard");
    }
    
}
