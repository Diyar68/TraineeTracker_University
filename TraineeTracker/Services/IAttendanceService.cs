namespace TraineeTracker.Services;

public class AttendanceResult
{
    public decimal? AttendedPT { get; set; }
    public bool IsApiAvailable { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IAttendanceService
{
    Task<AttendanceResult> GetAttendedPersonDaysAsync(
        string traineeId,
        string email,
        DateTime startDate,
        DateTime endDate);
}
