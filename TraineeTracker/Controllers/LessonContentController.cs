using Microsoft.AspNetCore.Mvc;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Filters;

namespace TraineeTracker.Controllers;

/// <summary>Local fictional replacement for the external company card deck.</summary>
[RequireDemoRole(AppConstants.RoleValues.Trainee, AppConstants.RoleValues.Mentor, AppConstants.RoleValues.Admin)]
[Route("DemoLessons")]
public class LessonContentController(ILectionRepository lessons) : Controller
{
    [HttpGet("{externalId:long}")]
    public async Task<IActionResult> Details(long externalId)
    {
        var lesson = (await lessons.GetAllAsync()).FirstOrDefault(item => item.ExternalId == externalId);
        if (lesson == null) return NotFound();
        return View(lesson);
    }
}
