using TraineeTracker.Models;
using TraineeTracker.Services;
using TraineeTracker.ViewModels;

namespace TraineeTracker.UnitTests;

/// <remarks>
/// </remarks>
public class DashboardGroupingServiceTests
{
    private readonly DashboardGroupingService _service = new();

    [Fact]
    public void BuildGroupedLections_Sorts_ActionRequiredStatuses_ByPriorityThenCurriculumOrder()
    {
        var items = new List<LectionWithProgress>
        {
            Item(5, LectionStatus.Started),
            Item(1, LectionStatus.Open),
            Item(4, LectionStatus.Finished),
            Item(7, LectionStatus.Rated),
            Item(3, LectionStatus.Accepted),
            Item(6, LectionStatus.Skipped),
            Item(2, LectionStatus.Rejected),
            Item(1, LectionStatus.Started),
        };

        var groups = _service.BuildGroupedLections(items);

        var actionRequired = groups.Single(g => g.Label == "Handlungsbedarf");
        Assert.Equal(
            [
                "02:rejected",
                "01:started",
                "05:started",
            ],
            actionRequired.Items.Select(i => $"{i.Lection.Number}:{i.StatusKey}"));
    }

    [Fact]
    public void BuildGroupedLections_UsesExpectedGroupOrderForAllStatuses()
    {
        var groups = _service.BuildGroupedLections(
        [
            Item(7, LectionStatus.Skipped),
            Item(4, LectionStatus.Accepted),
            Item(2, LectionStatus.Started),
            Item(6, LectionStatus.Rated),
            Item(1, LectionStatus.Rejected),
            Item(5, LectionStatus.Finished),
            Item(3, LectionStatus.Open),
        ]);

        Assert.Equal(
            [
                "Handlungsbedarf:rejected,started",
                "Als Nächstes:open",
                "Feedback offen:accepted",
                "Wartet auf Review:finished",
                "Abgeschlossen:rated",
                "Übersprungen:skipped",
            ],
            groups.Select(group => $"{group.Label}:{string.Join(",", group.Items.Select(item => item.StatusKey))}"));
    }

    [Fact]
    public void BuildGroupedLections_Keeps_OpenLessons_InCurriculumOrder()
    {
        var items = new List<LectionWithProgress>
        {
            Item(12, LectionStatus.Open),
            Item(3, LectionStatus.Open),
            Item(8, LectionStatus.Open),
        };

        var groups = _service.BuildGroupedLections(items);

        var next = groups.Single(g => g.Label == "Als Nächstes");
        Assert.Equal(["03", "08", "12"], next.Items.Select(i => i.Lection.Number));
    }

    [Fact]
    public void BuildGroupedLections_Omits_EmptyGroups()
    {
        var groups = _service.BuildGroupedLections(new List<LectionWithProgress>
        {
            Item(1, LectionStatus.Rated),
        });

        Assert.Single(groups);
        Assert.Equal("Abgeschlossen", groups.Single().Label);
    }

    [Theory]
    [InlineData(LectionStatus.Rejected, "Handlungsbedarf", 0, true)]
    [InlineData(LectionStatus.Started, "Handlungsbedarf", 0, true)]
    [InlineData(LectionStatus.Open, "Als Nächstes", 1, false)]
    [InlineData(LectionStatus.Accepted, "Feedback offen", 2, false)]
    [InlineData(LectionStatus.Finished, "Wartet auf Review", 3, false)]
    [InlineData(LectionStatus.Rated, "Abgeschlossen", 4, false)]
    [InlineData(LectionStatus.Skipped, "Übersprungen", 5, false)]
    public void StatusMetadata_MatchesDashboardGroups(
        LectionStatus status,
        string expectedLabel,
        int expectedOrder,
        bool expectedPriorityUsage)
    {
        Assert.Equal(expectedLabel, _service.GetGroupLabel(status));
        Assert.Equal(expectedOrder, _service.GetGroupOrder(status));
        Assert.Equal(expectedPriorityUsage, _service.UsesStatusPriority(status));
    }

    [Theory]
    [InlineData(LectionStatus.Rejected, LectionStatus.Started)]
    [InlineData(LectionStatus.Started, LectionStatus.Open)]
    [InlineData(LectionStatus.Open, LectionStatus.Accepted)]
    [InlineData(LectionStatus.Accepted, LectionStatus.Finished)]
    [InlineData(LectionStatus.Finished, LectionStatus.Rated)]
    [InlineData(LectionStatus.Rated, LectionStatus.Skipped)]
    public void StatusPriority_MatchesTraineeWorkflowOrder(
        LectionStatus higherPriority,
        LectionStatus lowerPriority)
    {
        Assert.True(
            _service.GetStatusPriority(higherPriority) < _service.GetStatusPriority(lowerPriority),
            $"{higherPriority} should be sorted before {lowerPriority}.");
    }

    private static LectionWithProgress Item(int number, LectionStatus status)
    {
        var lection = new Lection
        {
            Id = number,
            Number = number.ToString("00"),
            Title = $"Lektion {number}",
            Effort = 1m,
            OrderIndex = number,
        };

        var progress = status == LectionStatus.Open
            ? null
            : new LectionProgress
            {
                TraineeId = "test-trainee",
                LectionId = number,
                Status = status,
            };

        return new LectionWithProgress(lection, progress);
    }
}
