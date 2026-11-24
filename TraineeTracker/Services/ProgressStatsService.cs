using TraineeTracker.Models;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Services;

public class ProgressStatsService
{
    /// <remarks>Codeownership: Florian Pausch</remarks>
    public DashboardStats BuildStats(List<LectionWithProgress> items, decimal? attendedPT, bool isApiAvailable = true)
    {
        return ProgressMetricsCalculator.Calculate(items, attendedPT, isApiAvailable);
    }
}
