using TraineeTracker.Models;

namespace TraineeTracker.ViewModels;

// Model für die Mentor-Übersicht ("Meine Trainees", neuestes Feedback)

public class MentorDashboardViewModel
{
    public List<Lection> Lessons { get; init; } = new();
    public List<MentorTraineeCardViewModel> Trainees { get; init; } = new();
    public List<FeedbackEntryViewModel> LatestFeedback { get; init; } = new();
}

public class MentorTraineeCardViewModel
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Initials { get; init; } = "";
    public string ApiEmail { get; init; } = "";
    public int ProgressPercent { get; init; }
    public string BufferText { get; init; } = "";
    public bool BufferIsNegative { get; init; }
    public int AttentionCount { get; init; }
}
