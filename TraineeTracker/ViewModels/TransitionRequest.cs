namespace TraineeTracker.ViewModels
{
    /// <remarks>
    /// </remarks>
    public record TransitionRequest(
        int LectionId,
        string To,
        string? RejectionNote
    );
}
