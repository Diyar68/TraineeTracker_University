namespace TraineeTracker.Services;

public interface IDemoRoleService
{
    string? CurrentRole { get; }
    bool IsMentorOrAdmin();
}
