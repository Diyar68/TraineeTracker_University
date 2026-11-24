namespace TraineeTracker.Services;

public sealed class DemoMode
{
    // Deliberately read only the environment, never appsettings or command-line overrides.
    public bool Enabled { get; } = string.Equals(
        Environment.GetEnvironmentVariable("Demo__Enabled"), "true", StringComparison.OrdinalIgnoreCase);
}

public sealed class SyntheticAttendanceService : IAttendanceService
{
    public Task<AttendanceResult> GetAttendedPersonDaysAsync(
        string traineeId, string email, DateTime startDate, DateTime endDate)
    {
        var days = 0;
        for (var day = startDate.Date; day <= endDate.Date && day <= DateTime.Today; day = day.AddDays(1))
            if (day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                days++;
        return Task.FromResult(new AttendanceResult { AttendedPT = decimal.Round(days * 0.9m, 1), IsApiAvailable = true });
    }
}
