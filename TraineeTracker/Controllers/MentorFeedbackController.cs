using Microsoft.AspNetCore.Mvc;
using TraineeTracker.Filters;
using TraineeTracker.Services;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Controllers;

/// <summary>
/// Regelt die Feedback-Seiten und Feedback-Aktionen für Mentoren und Admins.
/// </summary>
[RequireDemoRole(AppConstants.RoleValues.Mentor, AppConstants.RoleValues.Admin)]
public class MentorFeedbackController : Controller
{
    private readonly DemoFeedbackService _feedbackService;

    /// <summary>
    /// Erstellt den Controller mit dem Feedback-Service.
    /// </summary>
    /// <param name="feedbackService">Service für die Feedback-Logik.</param>
    public MentorFeedbackController(DemoFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    /// <summary>
    /// Ändert per AJAX ein Feedback aus Mentor-Sicht.
    /// </summary>
    /// <param name="id">Id des Feedbacks.</param>
    /// <param name="request">Die neuen Werte für das Feedback.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateAjax(int id, [FromBody] DashboardFeedbackUpdateRequest request)
    {
        if (!_feedbackService.UpdateMentorFeedback(id, request.Difficulty, request.PriorKnowledge, request.Effort, request.Comment))
            return NotFound();

        var updated = _feedbackService.FindMentorFeedbackOrDefault(id);
        return Ok(new { feedback = updated });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    /// <summary>
    /// Löscht per AJAX ein Feedback aus Mentor-Sicht.
    /// </summary>
    /// <param name="id">Id des Feedbacks.</param>
    public IActionResult DeleteAjax(int id)
    {
        var result = _feedbackService.DeleteMentorFeedback(id);
        if (result == null) return NotFound();

        return Ok(new { deletion = result });
    }

}
