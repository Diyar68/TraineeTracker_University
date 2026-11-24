// Diyar
using TraineeTracker.Models;

namespace TraineeTracker.ViewModels
{
    // ViewModels/TraineeDashboardViewModel.cs
    /// <remarks>
    /// Codeownership: Diyar Altintas
    /// </remarks>
    public class TraineeDashboardViewModel
    {
        public string TraineeDisplayName { get; init; } = "";
        public List<LectionGroup> Groups { get; init; } = new();
        public DashboardStats Stats { get; init; } = new();
        public StatisticsDisplayViewModel StatisticsDisplay => StatisticsDisplayViewModel.From(Stats);
        public WeeklyPlanViewModel WeeklyPlan { get; init; } = new();
        public TraineeDashboardFeedbackViewModel Feedback { get; init; } = new();
        public MentorDashboardViewModel Mentor { get; init; } = new();
        public List<Lection> Lessons { get; init; } = new();
    }

    public class TraineeDashboardFeedbackViewModel
    {
        public List<FeedbackEntryViewModel> OwnFeedback { get; init; } = new();
        public List<FeedbackEntryViewModel> OthersFeedback { get; init; } = new();
        public List<FeedbackEntryViewModel> MentorFeedback { get; init; } = new();
        public List<LectionWithProgress> AcceptedLessons { get; init; } = new();
    }
}
