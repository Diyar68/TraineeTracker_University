namespace TraineeTracker.Services;

public interface ICurrentUserService
{
    string TraineeId { get; }
    string DisplayName { get; }
    string Email { get; }
    DateTime TraineeStartDate { get; }
    DateTime TraineeEndDate { get; }
}
