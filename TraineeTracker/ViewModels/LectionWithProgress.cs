using TraineeTracker.Models;

namespace TraineeTracker.ViewModels
{
    /// <remarks>
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
