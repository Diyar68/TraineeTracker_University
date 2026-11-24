namespace TraineeTracker.Services;

/// <summary>Deterministic attendance fixtures with weekdays, leave and fractional days.</summary>
public sealed class SyntheticAttendanceService : IAttendanceService
{
    public Task<AttendanceResult> GetAttendedPersonDaysAsync(
        string traineeId, string email, DateTime startDate, DateTime endDate)
    {
        decimal days = 0;
        var lastDay = endDate.Date < DateTime.Today ? endDate.Date : DateTime.Today;
        for (var date = startDate.Date; date <= lastDay; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || date.Day % 17 == 0)
                continue;
            days += date.Day % 11 == 0 ? 0.5m : 1m;
        }
        return Task.FromResult(new AttendanceResult { AttendedPT = days, IsApiAvailable = true });
    }
}
