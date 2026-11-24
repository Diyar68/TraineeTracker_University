// Diyar
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

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
[RequireDemoRole(AppConstants.RoleValues.Trainee, AppConstants.RoleValues.Mentor, AppConstants.RoleValues.Admin)]
public class ProgressController : Controller
{
    private readonly ILectionRepository _lectionRepo;
    private readonly ILectionProgressRepository _progressRepo;
    private readonly ILectionTransitionService _transitionService;
    private readonly ProgressStatsService _statsService;
    private readonly DashboardGroupingService _groupingService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDemoRoleService _demoRoleService;
    private readonly IAttendanceService _attendanceService;
    private readonly ICompositeViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly FeedbackRepository _feedbackRepo;

    public ProgressController(
        ILectionRepository lectionRepo,
        ILectionProgressRepository progressRepo,
        ILectionTransitionService transitionService,
        ProgressStatsService statsService,
        DashboardGroupingService groupingService,
        ICurrentUserService currentUser,
        IDemoRoleService demoRoleService,
        IAttendanceService attendanceService,
        ICompositeViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        FeedbackRepository feedbackRepo)
    {
        _lectionRepo = lectionRepo;
        _progressRepo = progressRepo;
        _transitionService = transitionService;
        _statsService = statsService;
        _groupingService = groupingService;
        _currentUser = currentUser;
        _demoRoleService = demoRoleService;
        _attendanceService = attendanceService;
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _feedbackRepo = feedbackRepo;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transition(
        [FromBody] TransitionRequest request)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.TraineeId)) return StatusCode(403);
        if (!Enum.TryParse<LectionStatus>(request.To, ignoreCase: true, out var toStatus))
            return BadRequest(new { error = "Ungültiger Zielstatus." });

        var lection = await _lectionRepo.GetByIdAsync(request.LectionId);
        if (lection == null) return NotFound();

        var progress = await _progressRepo.GetByLectionIdAsync(request.LectionId, _currentUser.TraineeId)
            ?? new LectionProgress
            {
                TraineeId = _currentUser.TraineeId,
                LectionId = request.LectionId,
                Status = LectionStatus.Open,
            };

        // Rolle temporär aus Demo-Client-State lesen, bis Identity eingerichtet ist.
        var isMentor = _demoRoleService.IsMentorOrAdmin();
        var previousStatus = progress.Status;

        if (!_transitionService.IsTransitionAllowed(progress.Status, toStatus, isMentor))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Dieser Statuswechsel ist für die aktuelle Rolle nicht erlaubt." });

        // "Rated" darf für Trainees nur über die Feedback-Abgabe erreicht werden
        // (siehe FeedbackController.CreateAjax), damit nie ein Rated-Status ohne
        // zugehörigen Feedback-Eintrag entsteht. Mentoren dürfen den Status weiterhin frei setzen.
        if (toStatus == LectionStatus.Rated && !isMentor)
            return BadRequest(new { error = "Bitte zuerst Feedback über das Feedback-Formular abgeben." });

        if (toStatus == LectionStatus.Rejected &&
            string.IsNullOrWhiteSpace(request.RejectionNote))
            return BadRequest(new { error = "Ablehnungsgrund ist erforderlich." });

        _transitionService.ApplyTransition(progress, toStatus, request.RejectionNote);
        await _progressRepo.SaveAsync(progress);

        if (toStatus == LectionStatus.Rated)
            _feedbackRepo.CreateEmptyIfMissing(progress.Id);
        else if (previousStatus == LectionStatus.Rated)
            _feedbackRepo.DeleteForProgress(progress.Id);

        // Aktualisierte Daten direkt zurückgeben — kein zweiter Request nötig
        var available = _transitionService.GetAvailableTransitions(progress.Status, isMentor);
        var allLections = await _lectionRepo.GetAllAsync();
        var allProgress = await _progressRepo.GetByTraineeIdAsync(_currentUser.TraineeId);
        var items = allLections
            .Select(l => new LectionWithProgress(
                l,
                allProgress.FirstOrDefault(p => p.LectionId == l.Id)))
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

        return Ok(TransitionResultViewModel.From(
            lection,
            progress,
            previousStatus,
            stats,
            cardHtml,
            modalContentHtml));
    }

    private DateTime EffectiveAttendanceEndDate()
    {
        var today = DateTime.Today;
        return _currentUser.TraineeEndDate.Date < today
            ? _currentUser.TraineeEndDate.Date
            : today;
    }

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
