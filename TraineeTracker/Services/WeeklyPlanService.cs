using System.Globalization;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Services;

/// <remarks>
/// </remarks>
public class WeeklyPlanService
{
    private const int NumberOfWeeks = 6;

    private static readonly string[] MonthNames =
        ["Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez"];

    private static readonly string[] DayNames = ["Mo", "Di", "Mi", "Do", "Fr"];

    private readonly ILectionRepository _lectionRepo;
    private readonly ILectionProgressRepository _progressRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public WeeklyPlanService(
        ILectionRepository lectionRepo,
        ILectionProgressRepository progressRepo,
        ICurrentUserService currentUser)
        : this(lectionRepo, progressRepo, currentUser, TimeProvider.System)
    {
    }

    public WeeklyPlanService(
        ILectionRepository lectionRepo,
        ILectionProgressRepository progressRepo,
        ICurrentUserService currentUser,
        TimeProvider timeProvider)
    {
        _lectionRepo = lectionRepo;
        _progressRepo = progressRepo;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public Task<WeeklyPlanViewModel> BuildCurrentWeeklyPlanAsync()
    {
        var today = Today();
        return BuildWeeklyPlanAsync(GetMonday(today));
    }

    public async Task<WeeklyPlanViewModel> BuildWeeklyPlanAsync(DateOnly from)
    {
        var startMonday = GetMonday(from);
        var to = startMonday.AddDays(NumberOfWeeks * 7 - 3);
        var today = Today();
        var planningStart = startMonday <= today && today <= to
            ? today
            : startMonday;
        var shouldProjectLessons = to >= today;

        var dayMap = await BuildProjectedDayMapAsync(startMonday, to, planningStart, shouldProjectLessons);
        var weeks = BuildWeeks(startMonday, today, dayMap);

        return new WeeklyPlanViewModel
        {
            From = startMonday,
            To = to,
            WeekLabel = BuildWeekLabel(startMonday, to),
            HeaderDays = weeks.FirstOrDefault()?.Days ?? [],
            Weeks = weeks,
        };
    }

    private async Task<Dictionary<DateOnly, List<WeeklyPlanEntryViewModel>>> BuildProjectedDayMapAsync(
        DateOnly from,
        DateOnly to,
        DateOnly planningStart,
        bool shouldProjectLessons)
    {
        var lections = await _lectionRepo.GetAllAsync();
        var progresses = await _progressRepo.GetByTraineeIdAsync(_currentUser.TraineeId);
        var progressByLection = progresses.ToDictionary(p => p.LectionId);

        var lessons = lections
            .Select(l => new LectionWithProgress(
                l,
                progressByLection.GetValueOrDefault(l.Id)))
            .Where(x => x.Status is
                LectionStatus.Rejected or
                LectionStatus.Started or
                LectionStatus.Open)
            .OrderBy(x => CalendarPriority(x.Status))
            .ThenBy(x => TraineeOrder(x))
            .ThenBy(x => x.Lection.Number)
            .ToList();

        var workDays = EachDay(from, to)
            .Where(d => d.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday)
            .ToList();

        var dayMap = workDays.ToDictionary(
            d => d,
            _ => new List<WeeklyPlanEntryViewModel>());

        if (!shouldProjectLessons)
        {
            return dayMap;
        }

        var planningDays = workDays
            .Where(d => d >= planningStart)
            .ToList();

        var dayIndex = 0;
        foreach (var item in lessons)
        {
            var plannedDays = Math.Max(1, (int)Math.Ceiling(item.Lection.Effort));
            for (var i = 0; i < plannedDays && dayIndex < planningDays.Count; i++)
            {
                var day = planningDays[dayIndex];
                dayMap[day].Add(new WeeklyPlanEntryViewModel
                {
                    Id = item.Lection.Id,
                    Title = item.Lection.Title,
                    Status = item.Status.ToString().ToLower(),
                    CssClass = item.Status.ToString().ToLower(),
                    Effort = $"{item.Lection.Effort:0.#} PT",
                    Tooltip = $"{item.Lection.Title} · {item.Status.ToString().ToLower()} · {item.Lection.Effort:0.#} PT",
                });
                dayIndex++;
            }
        }

        return dayMap;
    }

    private static int CalendarPriority(LectionStatus status) => status switch
    {
        LectionStatus.Rejected => 0,
        LectionStatus.Started => 1,
        LectionStatus.Open => 2,
        _ => 9,
    };

    private static int TraineeOrder(LectionWithProgress item)
    {
        if (item.Progress?.IndividualOrderIndex is int individualOrderIndex)
        {
            return individualOrderIndex < 0 ? int.MaxValue : individualOrderIndex;
        }

        return item.Lection.OrderIndex < 0 ? int.MaxValue : item.Lection.OrderIndex;
    }

    private static IEnumerable<DateOnly> EachDay(DateOnly from, DateOnly to)
    {
        for (var day = from; day <= to; day = day.AddDays(1))
            yield return day;
    }

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetUtcNow().DateTime);

    private static DateOnly GetMonday(DateOnly date)
    {
        var daysUntilMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysUntilMonday);
    }

    private List<WeeklyPlanWeekViewModel> BuildWeeks(
        DateOnly startMonday,
        DateOnly today,
        Dictionary<DateOnly, List<WeeklyPlanEntryViewModel>> dayMap)
    {
        var weeks = new List<WeeklyPlanWeekViewModel>();

        for (int weekIndex = 0; weekIndex < NumberOfWeeks; weekIndex++)
        {
            var weekStart = startMonday.AddDays(weekIndex * 7);
            var weekEnd = weekStart.AddDays(4); // Mo-Fr
            var calendarWeek = ISOWeek.GetWeekOfYear(weekEnd);

            var days = new List<WeeklyPlanDayViewModel>();
            for (int dayIndex = 0; dayIndex < 5; dayIndex++) // Mo-Fr
            {
                var currentDate = weekStart.AddDays(dayIndex);
                var dayName = DayNames[dayIndex];
                var isToday = currentDate == today;
                var entries = dayMap.GetValueOrDefault(currentDate, new List<WeeklyPlanEntryViewModel>());
                var isPast = currentDate < today;

                days.Add(new WeeklyPlanDayViewModel
                {
                    Date = currentDate,
                    DayName = dayName,
                    IsToday = isToday,
                    IsPast = isPast,
                    Entries = entries
                });
            }

            weeks.Add(new WeeklyPlanWeekViewModel
            {
                CalendarWeek = calendarWeek,
                Days = days
            });
        }

        return weeks;
    }

    private string BuildWeekLabel(DateOnly from, DateOnly to)
    {
        var fromMonth = MonthNames[from.Month - 1];
        var toMonth = MonthNames[to.Month - 1];
        var fromYear = from.Year;
        var toYear = to.Year;

        if (from.Month == to.Month && from.Year == to.Year)
        {
            return $"{fromMonth} {from.Day}–{to.Day}, {fromYear}";
        }
        else if (from.Year == to.Year)
        {
            return $"{fromMonth} {from.Day} – {toMonth} {to.Day}, {fromYear}";
        }
        else
        {
            return $"{fromMonth} {from.Day}, {fromYear} – {toMonth} {to.Day}, {toYear}";
        }
    }
}
