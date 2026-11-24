using TraineeTracker.Models;
using TraineeTracker.Services;

namespace TraineeTracker.UnitTests;

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class TraineeTransitionServiceTests
{
    private readonly LectionTransitionService _service = new();

    [Theory]
    [InlineData(LectionStatus.Open, LectionStatus.Started)]
    [InlineData(LectionStatus.Started, LectionStatus.Finished)]
    [InlineData(LectionStatus.Rejected, LectionStatus.Finished)]
    [InlineData(LectionStatus.Accepted, LectionStatus.Rated)]
    public void IsTransitionAllowed_Allows_TraineeWorkflowTransitions(
        LectionStatus from,
        LectionStatus to)
    {
        var allowed = _service.IsTransitionAllowed(from, to, isMentor: false);

        Assert.True(allowed);
    }

    [Theory]
    [InlineData(LectionStatus.Finished, LectionStatus.Accepted)]
    [InlineData(LectionStatus.Finished, LectionStatus.Rejected)]
    [InlineData(LectionStatus.Open, LectionStatus.Skipped)]
    public void IsTransitionAllowed_Blocks_MentorOnlyTransitions_ForTrainee(
        LectionStatus from,
        LectionStatus to)
    {
        var allowed = _service.IsTransitionAllowed(from, to, isMentor: false);

        Assert.False(allowed);
    }

    [Fact]
    public void IsTransitionAllowed_Allows_MentorWorkflowTransitions_ForMentor()
    {
        Assert.True(_service.IsTransitionAllowed(LectionStatus.Finished, LectionStatus.Accepted, isMentor: true));
        Assert.True(_service.IsTransitionAllowed(LectionStatus.Finished, LectionStatus.Rejected, isMentor: true));
        Assert.True(_service.IsTransitionAllowed(LectionStatus.Open, LectionStatus.Skipped, isMentor: true));
    }

    [Fact]
    public void GetAvailableTransitions_Hides_MentorOnlyActions_ForTrainee()
    {
        var transitions = _service.GetAvailableTransitions(LectionStatus.Open, isMentor: false);

        Assert.Equal(["started"], transitions.Select(t => Value(t, "to")));
        Assert.Equal("Starten", Value(transitions.Single(), "label"));
    }

    [Fact]
    public void GetAvailableTransitions_Shows_ReviewActions_ForMentor()
    {
        var transitions = _service.GetAvailableTransitions(LectionStatus.Finished, isMentor: true);

        Assert.Equal(["open", "started", "accepted", "rejected", "rated", "skipped"], transitions.Select(t => Value(t, "to")));
        Assert.DoesNotContain(transitions, t => Value(t, "to") == "finished");
        Assert.Contains(transitions, t => Value(t, "to") == "accepted" && Value(t, "label") == "Als Akzeptiert markieren");
        Assert.Contains(transitions, t => Value(t, "to") == "rejected" && Value(t, "label") == "Als Abgelehnt markieren");
        Assert.Contains(transitions, t => Value(t, "to") == "started" && Value(t, "label") == "In Bearbeitung setzen");
    }

    [Fact]
    public void ApplyTransition_ToRejected_SetsStatusTimestampAndReason()
    {
        var progress = Progress(LectionStatus.Finished);

        _service.ApplyTransition(progress, LectionStatus.Rejected, "Bitte Tests ergaenzen.");

        Assert.Equal(LectionStatus.Rejected, progress.Status);
        Assert.NotNull(progress.RejectedAt);
        Assert.Equal("Bitte Tests ergaenzen.", progress.RejectionNote);
    }

    [Fact]
    public void ApplyTransition_ToRated_SetsDefaultRatingOnlyWhenMissing()
    {
        var unrated = Progress(LectionStatus.Accepted);
        var alreadyRated = Progress(LectionStatus.Accepted);
        alreadyRated.Rating = 5;

        _service.ApplyTransition(unrated, LectionStatus.Rated);
        _service.ApplyTransition(alreadyRated, LectionStatus.Rated);

        Assert.Equal(3, unrated.Rating);
        Assert.Equal(5, alreadyRated.Rating);
        Assert.NotNull(unrated.RatedAt);
    }

    [Fact]
    public void GetStatusChain_ForRejected_UsesRejectedAsCurrentStep()
    {
        var chain = _service.GetStatusChain(LectionStatus.Rejected);

        Assert.Equal(["open", "started", "finished", "rejected"], chain.Select(t => Value(t, "status")));
        Assert.True((bool)RawValue(chain.Last(), "isCurrent")!);
    }

    private static LectionProgress Progress(LectionStatus status)
    {
        return new LectionProgress
        {
            TraineeId = "test-trainee",
            LectionId = 1,
            Status = status,
        };
    }

    private static string Value(object item, string propertyName)
    {
        return RawValue(item, propertyName)?.ToString() ?? "";
    }

    private static object? RawValue(object item, string propertyName)
    {
        return item.GetType().GetProperty(propertyName)?.GetValue(item);
    }
}
