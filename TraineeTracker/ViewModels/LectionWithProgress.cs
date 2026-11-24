// Diyar
using TraineeTracker.Models;

namespace TraineeTracker.ViewModels
{
    /// <remarks>
    /// Codeownership: Diyar Altintas
    /// </remarks>
    public class LectionWithProgress
    {
        public Lection Lection { get; }
        public LectionProgress? Progress { get; }

        public LectionStatus Status => Progress?.Status ?? LectionStatus.Open;

        public LectionWithProgress(Lection lection, LectionProgress? progress)
        {
            Lection = lection;
            Progress = progress;
        }
    }
}
