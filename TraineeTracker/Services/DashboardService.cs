using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Services;

/// <remarks>
/// </remarks>
public class DashboardService
{
    private readonly ILectionRepository _lectionRepo;
    private readonly ILectionProgressRepository _progressRepo;
    private readonly DashboardGroupingService _groupingService;
    private readonly ProgressStatsService _statsService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAttendanceService _attendanceService;

    public DashboardService(
        ILectionRepository lectionRepo,
        ILectionProgressRepository progressRepo,
        DashboardGroupingService groupingService,
        ProgressStatsService statsService,
        ICurrentUserService currentUser,
        IAttendanceService attendanceService)
    {
        _lectionRepo = lectionRepo;
        _progressRepo = progressRepo;
        _groupingService = groupingService;
        _statsService = statsService;
        _currentUser = currentUser;
        _attendanceService = attendanceService;
    }

    public async Task<TraineeDashboardViewModel> BuildDashboardAsync()
    {
        var lections = await _lectionRepo.GetAllAsync();
        var progress = await _progressRepo.GetByTraineeIdAsync(_currentUser.TraineeId);

        var items = lections
            .Select(l => new LectionWithProgress(
                l,
                progress.FirstOrDefault(p => p.LectionId == l.Id)))
            .ToList();

        var attendance = await _attendanceService.GetAttendedPersonDaysAsync(
            _currentUser.TraineeId,
            _currentUser.Email,
            _currentUser.TraineeStartDate,
            EffectiveAttendanceEndDate());

        return new TraineeDashboardViewModel
        {
            TraineeDisplayName = _currentUser.DisplayName,
            Groups = _groupingService.BuildGroupedLections(items),
            Stats = _statsService.BuildStats(items, attendance.AttendedPT, attendance.IsApiAvailable),
            Lessons = lections,
        };
    }

    private DateTime EffectiveAttendanceEndDate()
    {
        var today = DateTime.Today;
        return _currentUser.TraineeEndDate.Date < today
            ? _currentUser.TraineeEndDate.Date
            : today;
    }
}
