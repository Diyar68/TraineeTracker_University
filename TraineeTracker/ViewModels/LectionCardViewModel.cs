using TraineeTracker.Helpers;
using TraineeTracker.Models;
using TraineeTracker.Services;

namespace TraineeTracker.ViewModels;

/// <remarks>
/// </remarks>
public class LectionCardViewModel
{
    public Lection Lection { get; }
    public LectionProgress? Progress { get; }
    public LectionStatus Status { get; }
    public string StatusKey { get; }
    public string StatusLabel { get; }
    public string StatusCssClass { get; }
    public string GroupLabel { get; }
    public int GroupOrder { get; }
    public int SortPriority { get; }
    public bool GroupUsesPriority { get; }
    public string GroupUsesPriorityValue { get; }
    public string MetaText { get; }
    public string SearchText { get; }
    public string EffortText { get; }
    public bool HasRejectionNote { get; }
    public string? RejectionNote { get; }

    public LectionCardViewModel(LectionWithProgress item, DashboardGroupingService groupingService)
    {
        Lection = item.Lection;
        Progress = item.Progress;
        Status = item.Status;
        StatusKey = Status.ToString().ToLowerInvariant();
        StatusCssClass = StatusKey;
        StatusLabel = Status.DisplayName();
        GroupLabel = groupingService.GetGroupLabel(Status);
        GroupOrder = groupingService.GetGroupOrder(Status);
        SortPriority = groupingService.GetStatusPriority(Status);
        GroupUsesPriority = groupingService.UsesStatusPriority(Status);
        GroupUsesPriorityValue = GroupUsesPriority.ToString().ToLowerInvariant();
        MetaText = BuildMetaText(Status, Progress);
        SearchText = $"{Lection.Title} {StatusKey} {StatusLabel}".ToLowerInvariant();
        EffortText = $"{Lection.Effort:0.0} PT";
        RejectionNote = Progress?.RejectionNote;
        HasRejectionNote = Status == LectionStatus.Rejected && !string.IsNullOrEmpty(RejectionNote);
    }

    private static string BuildMetaText(LectionStatus status, LectionProgress? progress) => status switch
    {
        LectionStatus.Rejected =>
            $"Abgelehnt am {progress?.RejectedAt?.ToString("dd.MM.yyyy") ?? "–"}",

        LectionStatus.Finished =>
            $"Abgeschlossen am {progress?.FinishedAt?.ToString("dd.MM.yyyy") ?? "–"} · Wartet auf Review",

        LectionStatus.Accepted =>
            $"Review bestanden am {progress?.AcceptedAt?.ToString("dd.MM.yyyy") ?? "–"} · Feedback ausstehend",

        LectionStatus.Started =>
            $"Gestartet am {progress?.StartedAt?.ToString("dd.MM.yyyy") ?? "–"}",

        LectionStatus.Rated =>
            $"Bewertet am {progress?.RatedAt?.ToString("dd.MM.yyyy") ?? "–"} "
            + new string('★', progress?.Rating ?? 0)
            + new string('☆', 5 - (progress?.Rating ?? 0)),

        LectionStatus.Skipped =>
            $"Übersprungen am {progress?.SkippedAt?.ToString("dd.MM.yyyy") ?? "–"}",

        _ => "–",
    };
}
