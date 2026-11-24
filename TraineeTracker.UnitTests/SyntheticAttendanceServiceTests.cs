using TraineeTracker.Services;

namespace TraineeTracker.UnitTests;

public class SyntheticAttendanceServiceTests
{
    [Theory]
    [InlineData(2020, 1, 6, 2020, 1, 10, 5)]
    [InlineData(2020, 1, 13, 2020, 1, 17, 4)]
    [InlineData(2020, 2, 11, 2020, 2, 11, 0.5)]
    [InlineData(2020, 1, 11, 2020, 1, 12, 0)]
    [InlineData(2020, 1, 12, 2020, 1, 11, 0)]
    public async Task CalculatesSyntheticAttendance(int sy, int sm, int sd, int ey, int em, int ed, decimal expected)
    {
        var result = await new SyntheticAttendanceService().GetAttendedPersonDaysAsync(
            "fictional-trainee", "trainee@demo.example", new DateTime(sy, sm, sd), new DateTime(ey, em, ed));
        Assert.True(result.IsApiAvailable);
        Assert.Equal(expected, result.AttendedPT);
    }
}
