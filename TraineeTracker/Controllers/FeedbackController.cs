using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Filters;
using TraineeTracker.Models;
using TraineeTracker.Services;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Controllers;

/// <summary>
/// Regelt die Feedback-Seiten und Feedback-Aktionen für Trainees.
/// </summary>
[RequireDemoRole(AppConstants.RoleValues.Trainee)]
public class FeedbackController : Controller
{
    private readonly DemoFeedbackService _feedbackService;
    private readonly ILectionRepository _lectionRepo;
    private readonly ILectionProgressRepository _progressRepo;
    private readonly ILectionTransitionService _transitionService;
    private readonly ProgressStatsService _statsService;
    private readonly DashboardGroupingService _groupingService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAttendanceService _attendanceService;
    private readonly ICompositeViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;

    /// <summary>
    /// Erstellt den Controller mit den benötigten Services und Repositories.
    /// </summary>
    /// <param name="feedbackService">Service für die Feedback-Logik.</param>
    /// <param name="lectionRepo">Repository für Lektionen.</param>
    /// <param name="progressRepo">Repository für den Lernfortschritt.</param>
    /// <param name="transitionService">Service für Statuswechsel.</param>
    /// <param name="statsService">Service für Fortschritts-Statistiken.</param>
    /// <param name="groupingService">Service für die Gruppierung im Dashboard.</param>
    /// <param name="currentUser">Aktuell angemeldeter Benutzer.</param>
    /// <param name="attendanceService">Service für Anwesenheitsdaten.</param>
    /// <param name="viewEngine">View-Engine zum Rendern von Partial Views.</param>
    /// <param name="tempDataProvider">Provider für TempData.</param>
    public FeedbackController(
        DemoFeedbackService feedbackService,
        ILectionRepository lectionRepo,
        ILectionProgressRepository progressRepo,
        ILectionTransitionService transitionService,
        ProgressStatsService statsService,
        DashboardGroupingService groupingService,
        ICurrentUserService currentUser,
        IAttendanceService attendanceService,
        ICompositeViewEngine viewEngine,
        ITempDataProvider tempDataProvider)
    {
        _feedbackService = feedbackService;
        _lectionRepo = lectionRepo;
        _progressRepo = progressRepo;
        _transitionService = transitionService;
        _statsService = statsService;
        _groupingService = groupingService;
        _currentUser = currentUser;
        _attendanceService = attendanceService;
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
    }

    /// <summary>
    /// Legt per AJAX ein neues Feedback für eine akzeptierte Lektion an.
    /// </summary>
    /// <param name="request">Die Daten aus dem Feedback-Formular.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAjax([FromBody] DashboardFeedbackCreateRequest request)
    {
        var feedback = await _feedbackService.CreateOwnFeedbackForLectionAsync(
            request.LectionId, request.Difficulty, request.PriorKnowledge, request.Effort, request.Comment);

        if (feedback == null)
            return BadRequest(new { error = "Für diese Lektion kann aktuell kein Feedback abgegeben werden." });

        var transition = await BuildTransitionResultAsync(request.LectionId, LectionStatus.Accepted);
        if (transition == null) return NotFound();

        return Ok(new { feedback, transition });
    }

    /// <summary>
    /// Ändert per AJAX ein bestehendes eigenes Feedback.
    /// </summary>
    /// <param name="id">Id des Feedbacks.</param>
    /// <param name="request">Die neuen Werte für das Feedback.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateAjax(int id, [FromBody] DashboardFeedbackUpdateRequest request)
    {
        if (!_feedbackService.UpdateOwnFeedback(id, request.Difficulty, request.PriorKnowledge, request.Effort, request.Comment))
            return NotFound();

        var updated = _feedbackService.FindOwnFeedbackOrDefault(id);
        return Ok(new { feedback = updated });
    }

    /// <summary>
    /// Löscht per AJAX ein eigenes Feedback.
    /// </summary>
    /// <param name="id">Id des Feedbacks.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAjax(int id)
    {
        var result = _feedbackService.DeleteOwnFeedback(id);
        if (result == null) return NotFound();

        var transition = await BuildTransitionResultAsync(result.LectionId, LectionStatus.Rated);
        return Ok(new { deletion = result, transition });
    }

    /// <summary>
    /// Baut das Ergebnis für die Anzeige nach einem Statuswechsel zusammen.
    /// </summary>
    /// <param name="lectionId">Id der betroffenen Lektion.</param>
    /// <param name="previousStatus">Status der Lektion vor der Änderung.</param>
    private async Task<TransitionResultViewModel?> BuildTransitionResultAsync(int lectionId, LectionStatus previousStatus)
    {
        var lection = await _lectionRepo.GetByIdAsync(lectionId);
        var progress = await _progressRepo.GetByLectionIdAsync(lectionId, _currentUser.TraineeId);
        if (lection == null || progress == null) return null;

        var available = _transitionService.GetAvailableTransitions(progress.Status, isMentor: false);
        var allLections = await _lectionRepo.GetAllAsync();
        var allProgress = await _progressRepo.GetByTraineeIdAsync(_currentUser.TraineeId);
        var items = allLections
            .Select(l => new LectionWithProgress(l, allProgress.FirstOrDefault(p => p.LectionId == l.Id)))
            .ToList();
        var attendance = await _attendanceService.GetAttendedPersonDaysAsync(
            _currentUser.TraineeId,
            _currentUser.Email,
            _currentUser.TraineeStartDate,
            EffectiveAttendanceEndDate());
        var stats = _statsService.BuildStats(items, attendance.AttendedPT, attendance.IsApiAvailable);
        var card = new LectionCardViewModel(new LectionWithProgress(lection, progress), _groupingService);
        var cardHtml = await RenderPartialToStringAsync("_LectionCard", card);
        var statusChain = _transitionService.GetStatusChain(progress.Status);
        var modal = LessonModalViewModel.From(
            lection,
            progress,
            progress.Status,
            statusChain,
            available);
        var modalContentHtml = await RenderPartialToStringAsync("_LessonModalContent", modal);

        return TransitionResultViewModel.From(
            lection,
            progress,
            previousStatus,
            stats,
            cardHtml,
            modalContentHtml);
    }

    /// <summary>
    /// Gibt das Enddatum für die Anwesenheitsberechnung zurück.
    /// </summary>
    private DateTime EffectiveAttendanceEndDate()
    {
        var today = DateTime.Today;
        return _currentUser.TraineeEndDate.Date < today
            ? _currentUser.TraineeEndDate.Date
            : today;
    }

    /// <summary>
    /// Rendert eine Partial View als HTML-Text.
    /// </summary>
    /// <param name="viewName">Name der Partial View.</param>
    /// <param name="model">Model für die View.</param>
    private async Task<string> RenderPartialToStringAsync(string viewName, object model)
    {
        var viewResult = _viewEngine.FindView(ControllerContext, viewName, isMainPage: false);
        if (!viewResult.Success && viewName == "_LessonModalContent")
            viewResult = _viewEngine.GetView(null, "~/Views/TraineeDashboard/_LessonModalContent.cshtml", isMainPage: false);

        if (!viewResult.Success)
            throw new InvalidOperationException($"Partial View '{viewName}' wurde nicht gefunden.");

        await using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var viewData = new ViewDataDictionary(ViewData)
        {
            Model = model,
        };
        var tempData = new TempDataDictionary(HttpContext, _tempDataProvider);
        var viewContext = new ViewContext(
            ControllerContext,
            viewResult.View,
            viewData,
            tempData,
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }

}
