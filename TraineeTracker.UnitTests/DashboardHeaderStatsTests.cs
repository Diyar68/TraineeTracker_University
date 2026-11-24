using TraineeTracker.Models;
using TraineeTracker.ViewModels;

namespace TraineeTracker.UnitTests;

/// <remarks>
/// </remarks>
public class DashboardHeaderStatsTests
{
    [Fact]
    public void AttentionCount_CountsOnlyStartedAndRejected()
    {
        var stats = Stats(
            (LectionStatus.Started, 2),
            (LectionStatus.Rejected, 3),
            (LectionStatus.Finished, 5),
            (LectionStatus.Accepted, 7));

        Assert.Equal(5, stats.AttentionCount);
        Assert.Equal(5, stats.ReviewCount);
    }

    [Theory]
    [InlineData(LectionStatus.Open, LectionStatus.Started, 0, 0, 1, 0)]
    [InlineData(LectionStatus.Started, LectionStatus.Finished, 1, 0, 0, 1)]
    [InlineData(LectionStatus.Finished, LectionStatus.Rejected, 0, 1, 1, 0)]
    [InlineData(LectionStatus.Finished, LectionStatus.Accepted, 0, 1, 0, 0)]
    [InlineData(LectionStatus.Rejected, LectionStatus.Started, 1, 0, 1, 0)]
    [InlineData(LectionStatus.Accepted, LectionStatus.Rated, 0, 0, 0, 0)]
    public void TransitionStats_ReflectExpectedHeaderCountsAfterStatusChange(
        LectionStatus from,
        LectionStatus to,
        int expectedAttentionBefore,
        int expectedReviewBefore,
        int expectedAttentionAfter,
        int expectedReviewAfter)
    {
        var before = TransitionStatsViewModel.From(Stats((from, 1)));
        var after = TransitionStatsViewModel.From(Stats((to, 1)));

        Assert.Equal(expectedAttentionBefore, before.AttentionCount);
        Assert.Equal(expectedReviewBefore, before.ReviewCount);
        Assert.Equal(expectedAttentionAfter, after.AttentionCount);
        Assert.Equal(expectedReviewAfter, after.ReviewCount);
    }

    private static DashboardStats Stats(params (LectionStatus Status, int Count)[] counts)
    {
        return new DashboardStats
        {
            CountByStatus = counts.ToDictionary(item => item.Status, item => item.Count),
        };
    }
}
