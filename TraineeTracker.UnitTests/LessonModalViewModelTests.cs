using TraineeTracker.Models;
using TraineeTracker.Services;
using TraineeTracker.ViewModels;

namespace TraineeTracker.UnitTests;

/// <remarks>
/// </remarks>
public class LessonModalViewModelTests
{
    [Fact]
    public void From_PreparesModalContentForOpenLesson()
    {
        var service = new LectionTransitionService();
        var lection = Lection();
        var progress = Progress(LectionStatus.Open);

        var model = LessonModalViewModel.From(
            lection,
            progress,
            LectionStatus.Open,
            service.GetStatusChain(LectionStatus.Open),
            service.GetAvailableTransitions(LectionStatus.Open, isMentor: false));

        Assert.Equal(lection.Id, model.LectionId);
        Assert.Equal("Linux Grundlagen", model.Title);
        Assert.Equal("Lektion 01", model.EyebrowText);
        Assert.Equal("0.5 PT", model.EffortText);
        Assert.Equal("https://cards.example/linux", model.ExternalLink);
        Assert.Contains(model.StatusChain, node => node.Status == "open" && node.IsCurrent);
        Assert.Contains(model.Transitions, t => t.TestId == "transition-open-to-started" && t.Label == "Starten");
        Assert.DoesNotContain(model.Transitions, t => t.TestId == "transition-open-to-skipped");
    }

    [Fact]
    public void From_PreparesRejectionNoteForRejectedLesson()
    {
        var service = new LectionTransitionService();
        var progress = Progress(LectionStatus.Rejected);
        progress.RejectionNote = "Bitte nochmal überarbeiten.";

        var model = LessonModalViewModel.From(
            Lection(),
            progress,
            LectionStatus.Rejected,
            service.GetStatusChain(LectionStatus.Rejected),
            service.GetAvailableTransitions(LectionStatus.Rejected, isMentor: false));

        Assert.Equal("rejected", model.Status);
        Assert.Equal("Abgelehnt", model.StatusLabel);
        Assert.Equal("Bitte nochmal überarbeiten.", model.RejectionNote);
        Assert.Contains(model.StatusChain, node => node.Status == "rejected" && node.IsCurrent);
        Assert.Contains(model.Transitions, t => t.TestId == "transition-rejected-to-finished");
    }

    private static Lection Lection()
    {
        return new Lection
        {
            Id = 42,
            ExternalId = 142,
            Number = "01",
            Title = "Linux Grundlagen",
            Effort = 0.5m,
            ExternalLink = "https://cards.example/linux",
        };
    }

    private static LectionProgress Progress(LectionStatus status)
    {
        return new LectionProgress
        {
            TraineeId = "trainee-1",
            LectionId = 42,
            Status = status,
        };
    }
}
