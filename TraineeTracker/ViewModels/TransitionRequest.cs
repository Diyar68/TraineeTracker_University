// Diyar
namespace TraineeTracker.ViewModels
{
    /// <remarks>
    /// Codeownership: Diyar Altintas
    /// </remarks>
    public record TransitionRequest(
        int LectionId,
        string To,
        string? RejectionNote
    );
}
