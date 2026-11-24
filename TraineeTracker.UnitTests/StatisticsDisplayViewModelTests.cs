using TraineeTracker.ViewModels;

namespace TraineeTracker.UnitTests;
/// <remarks>Codeownership: Florian Pausch</remarks>
public class StatisticsDisplayViewModelTests
{
    [Fact]
    public void From_ShowsPlaceholderWhenApiUnavailable()
    {
        var stats = new DashboardStats
        {
            AttendedPT = null,
            DonePT = 5m,
            OpenPT = 5m,
            IsApiAvailable = false,
            BufferPT = null,
            VelocityPercent = null,
            ForecastBufferPT = null,
        };

        var result = StatisticsDisplayViewModel.From(stats);

        Assert.False(result.IsApiAvailable);
        Assert.Equal("–", result.AttendedText);
        Assert.Equal("–", result.BufferText);
        Assert.Equal("–", result.VelocityText);
        Assert.Equal("–", result.ForecastText);
        Assert.Equal("", result.BufferToneClass);
        Assert.Equal("", result.VelocityToneClass);
        Assert.Equal("", result.ForecastToneClass);
    }

    [Fact]
    public void From_FormatsPositiveBufferWithPlusSignAndPositiveTone()
    {
        var stats = new DashboardStats
        {
            AttendedPT = 10m,
            DonePT = 12m,
            OpenPT = 8m,
            BufferPT = 2m,
            VelocityPercent = 120m,
            ForecastBufferPT = 3.333m,
        };

        var result = StatisticsDisplayViewModel.From(stats);

        Assert.Equal("+2,0 PT", result.BufferText);
        Assert.Equal("positive", result.BufferToneClass);
        Assert.Equal("120%", result.VelocityText);
        Assert.Equal("positive", result.VelocityToneClass);
        Assert.Equal("+3,3 PT", result.ForecastText);
        Assert.Equal("positive", result.ForecastToneClass);
    }

    [Fact]
    public void From_FormatsNegativeBufferWithoutPlusSignAndNegativeTone()
    {
        var stats = new DashboardStats
        {
            AttendedPT = 10m,
            DonePT = 4m,
            OpenPT = 16m,
            BufferPT = -6m,
            VelocityPercent = 40m,
            ForecastBufferPT = -12.04m,
        };

        var result = StatisticsDisplayViewModel.From(stats);

        Assert.Equal("-6,0 PT", result.BufferText);
        Assert.Equal("negative", result.BufferToneClass);
        Assert.Equal("40%", result.VelocityText);
        Assert.Equal("negative", result.VelocityToneClass);
        Assert.Equal("-12,0 PT", result.ForecastText);
        Assert.Equal("negative", result.ForecastToneClass);
    }

    [Fact]
    public void From_TreatsExactlyOnTargetAsPositiveTone()
    {
        // 0 PT Puffer bzw. genau 100% Geschwindigskeit sollen noch grün sein, nicht rot
        var stats = new DashboardStats
        {
            AttendedPT = 10m,
            DonePT = 10m,
            OpenPT = 5m,
            BufferPT = 0m,
            VelocityPercent = 100m,
            ForecastBufferPT = 0m,
        };

        var result = StatisticsDisplayViewModel.From(stats);

        Assert.Equal("+0,0 PT", result.BufferText);
        Assert.Equal("positive", result.BufferToneClass);
        Assert.Equal("100%", result.VelocityText);
        Assert.Equal("positive", result.VelocityToneClass);
    }
}