using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Helpers;
using TraineeTracker.Models;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Services;

// Joel Bitterlich ganzer Code

// Baut die Mentor-Übersicht aus echten Datenbank-Daten auf. 
// Mentoren sehen dabei nur ihre zugewiesenen Trainees,
// Admins alle aktiven Trainees (siehe Admin-als-Mentor-Zugriff)
public class MentorDashboardService
{
    private readonly UserRepository _userRepository;
    private readonly MentorAssignmentRepository _mentorAssignmentRepo;
    private readonly ILectionRepository _lectionRepo;
    private readonly ILectionProgressRepository _progressRepo;
    private readonly ProgressStatsService _statsService;
    private readonly IAttendanceService _attendanceService;
    private readonly DemoFeedbackService _demoFeedbackService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MentorDashboardService(
        UserRepository userRepository,
        MentorAssignmentRepository mentorAssignmentRepo,
        ILectionRepository lectionRepo,
        ILectionProgressRepository progressRepo,
        ProgressStatsService statsService,
        IAttendanceService attendanceService,
        DemoFeedbackService demoFeedbackService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _mentorAssignmentRepo = mentorAssignmentRepo;
        _lectionRepo = lectionRepo;
        _progressRepo = progressRepo;
        _statsService = statsService;
        _attendanceService = attendanceService;
        _demoFeedbackService = demoFeedbackService;
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;
    private bool IsAdmin => string.Equals(Principal?.GetRole(), AppConstants.Roles.Admin, StringComparison.OrdinalIgnoreCase);
    private string? AuthenticatedUserId => Principal?.GetUserId();
    
    // Joel Bitterlich baut das ViewModel für MentorDashboard auf
    public async Task<MentorDashboardViewModel> BuildMentorDashboardAsync()
    {
        var trainees = GetRelevantTrainees();
        var lections = await _lectionRepo.GetAllAsync();

        var traineeCards = new List<MentorTraineeCardViewModel>();
        foreach (var trainee in trainees)
        {
            traineeCards.Add(await BuildTraineeCardAsync(trainee, lections));
        }

        return new MentorDashboardViewModel
        {
            Trainees = traineeCards,
            Lessons = lections,
            LatestFeedback = _demoFeedbackService.GetMentorFeedback()
                .OrderByDescending(feedback => feedback.SubmittedAt)
                .ToList(),
        };
    }

    // Joel Bitterlich ermittelt die zugewiseenen Trainees für Mentor und Admin alle Trainees
    private List<User> GetRelevantTrainees()
    {
        if (IsAdmin)
            return _userRepository.GetTrainees();

        if (AuthenticatedUserId == null)
            return new List<User>();

        var assignedIds = _mentorAssignmentRepo.GetTraineeIdsForMentor(AuthenticatedUserId).ToHashSet();
        return _userRepository.GetTrainees().Where(t => assignedIds.Contains(t.Id)).ToList();
    }

    // Joel Bitterlich baut die Trainee Übersicht Karte für das Dashboard auf
    private async Task<MentorTraineeCardViewModel> BuildTraineeCardAsync(User trainee, List<Lection> lections)
    {
        var progress = await _progressRepo.GetByTraineeIdAsync(trainee.Id);
        var items = lections
            .Select(l => new LectionWithProgress(l, progress.FirstOrDefault(p => p.LectionId == l.Id)))
            .ToList();

        var startDate = trainee.TraineeStartDate ?? DateTime.Today;
        var endDate = trainee.TraineeEndDate ?? DateTime.Today;
        var effectiveEnd = endDate.Date < DateTime.Today ? endDate.Date : DateTime.Today;

        var attendance = await _attendanceService.GetAttendedPersonDaysAsync(trainee.Id, trainee.Email, startDate, effectiveEnd);
        var stats = _statsService.BuildStats(items, attendance.AttendedPT, attendance.IsApiAvailable);
        var display = StatisticsDisplayViewModel.From(stats);
        var totalPlanPt = stats.DonePT + stats.OpenPT;
        var doneShare = totalPlanPt > 0
            ? Math.Min(
                AppConstants.Progress.PercentFactor,
                stats.DonePT / totalPlanPt * AppConstants.Progress.PercentFactor)
            : 0;

        return new MentorTraineeCardViewModel
        {
            Id = trainee.Id,
            Name = trainee.FullName,
            Initials = BuildInitials(trainee),
            ApiEmail = trainee.Email,
            ProgressPercent = (int)Math.Round(doneShare),
            BufferText = display.BufferText,
            BufferIsNegative = display.BufferToneClass == "negative",
            AttentionCount = stats.AttentionCount,
        };
    }

    // Joel Bitterlich baut die Intialdaten für die Trainee Karte
    private static string BuildInitials(User user)
    {
        var first = user.FirstName.Length > 0 ? user.FirstName[..1] : "";
        var last = user.LastName.Length > 0 ? user.LastName[..1] : "";
        return (first + last).ToUpperInvariant();
    }
}
