using TraineeTracker.Models;

namespace TraineeTracker.Services;

/// <remarks>
/// </remarks>
public interface ILectionTransitionService
{
    bool IsTransitionAllowed(LectionStatus from, LectionStatus to, bool isMentor);
    List<object> GetAvailableTransitions(LectionStatus from, bool isMentor);
    List<object> GetStatusChain(LectionStatus current);
    void ApplyTransition(LectionProgress progress, LectionStatus to, string? rejectionNote = null);
}
