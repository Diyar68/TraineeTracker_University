// Diyar
using Microsoft.AspNetCore.Mvc;
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
public class TraineeDashboardController : Controller
{
    private readonly ILectionRepository _lectionRepo;
    private readonly ILectionProgressRepository _progressRepo;
    private readonly ILectionTransitionService _transitionService;
    private readonly DashboardService _dashboardService;
    private readonly WeeklyPlanService _weeklyPlanService;
    private readonly DemoFeedbackService _demoFeedbackService;
    private readonly MentorDashboardService _mentorDashboardService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDemoRoleService _demoRoleService;

    public TraineeDashboardController(
        ILectionRepository lectionRepo,
        ILectionProgressRepository progressRepo,
        ILectionTransitionService transitionService,
        DashboardService dashboardService,
        WeeklyPlanService weeklyPlanService,
        DemoFeedbackService demoFeedbackService,
        MentorDashboardService mentorDashboardService,
        ICurrentUserService currentUser,
        IDemoRoleService demoRoleService)
    {
        _lectionRepo = lectionRepo;
        _progressRepo = progressRepo;
        _transitionService = transitionService;
        _dashboardService = dashboardService;
        _weeklyPlanService = weeklyPlanService;
        _demoFeedbackService = demoFeedbackService;
        _mentorDashboardService = mentorDashboardService;
        _currentUser = currentUser;
        _demoRoleService = demoRoleService;
    }

    // ═══════════════════════════════════════════════════════
    // DASHBOARD — HAUPTSEITE
    // ═══════════════════════════════════════════════════════

    public async Task<IActionResult> Index()
    {
        var mentorOverview = await _mentorDashboardService.BuildMentorDashboardAsync();

        if (_demoRoleService.IsMentorOrAdmin() && string.IsNullOrWhiteSpace(_currentUser.TraineeId))
        {
            return View(new TraineeDashboardViewModel
            {
                Mentor = mentorOverview,
                Lessons = mentorOverview.Lessons,
            });
        }

        var traineeDashboard = await _dashboardService.BuildDashboardAsync();

        return View(new TraineeDashboardViewModel
        {
            TraineeDisplayName = traineeDashboard.TraineeDisplayName,
            Groups = traineeDashboard.Groups,
            Stats = traineeDashboard.Stats,
            WeeklyPlan = await _weeklyPlanService.BuildCurrentWeeklyPlanAsync(),
            Feedback = await _demoFeedbackService.BuildDashboardFeedbackAsync(),
            Mentor = mentorOverview,
            Lessons = traineeDashboard.Lessons,
        });
    }

    // ═══════════════════════════════════════════════════════
    // MODAL — LECTION DETAIL
    // ═══════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> LectionDetail(int id)
    {
        var lection = await _lectionRepo.GetByIdAsync(id);
        var progress = await _progressRepo.GetByLectionIdAsync(id, _currentUser.TraineeId);
        if (lection == null) return NotFound();

        var item = new LectionWithProgress(lection, progress);
        var isMentor = _demoRoleService.IsMentorOrAdmin();
        var status = item.Status;
        var model = LessonModalViewModel.From(
            lection,
            progress,
            status,
            _transitionService.GetStatusChain(status),
            _transitionService.GetAvailableTransitions(status, isMentor));

        return PartialView("_LessonModalContent", model);
    }

    // ═══════════════════════════════════════════════════════
    // KALENDER — DATEN
    // ═══════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> WeeklyPlanPartial(DateOnly? from)
    {
        var model = from is { } start
            ? await _weeklyPlanService.BuildWeeklyPlanAsync(start)
            : await _weeklyPlanService.BuildCurrentWeeklyPlanAsync();

        return PartialView("_WeeklyPlanCalendar", model);
    }

   // ═══════════════════════════════════════════════════════
    // FEEDBACK PARTIAL
    // ═══════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> FeedbackPartial()
    {
        var model = new TraineeDashboardViewModel
        {
            Feedback = await _demoFeedbackService.BuildDashboardFeedbackAsync(),
        };

        return PartialView("_DashboardFeedbackView", model);
    }

}
