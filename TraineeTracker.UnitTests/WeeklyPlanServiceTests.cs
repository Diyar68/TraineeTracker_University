using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using TraineeTracker.Services;
using TraineeTracker.ViewModels;

namespace TraineeTracker.UnitTests;

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class WeeklyPlanServiceTests
{
    [Fact]
    public async Task BuildWeeklyPlanAsync_PlansRelevantLessons_ByPriorityAcrossWorkdays()
    {
        var service = CreateService(
            [
                Lection(3, "Open Lesson", 1m),
                Lection(2, "Started Lesson", 1.2m),
                Lection(5, "Rejected Lesson", 1m),
                Lection(8, "Accepted Lesson", 1m),
            ],
            [
                Progress(2, LectionStatus.Started),
                Progress(5, LectionStatus.Rejected),
                Progress(8, LectionStatus.Accepted),
            ]);

        var calendar = await service.BuildWeeklyPlanAsync(new DateOnly(2026, 7, 6));

        Assert.Equal("Rejected Lesson", Day(calendar, 2026, 7, 6).Entries.Single().Title);
        Assert.Equal("Started Lesson", Day(calendar, 2026, 7, 7).Entries.Single().Title);
        Assert.Equal("Started Lesson", Day(calendar, 2026, 7, 8).Entries.Single().Title);
        Assert.Equal("Open Lesson", Day(calendar, 2026, 7, 9).Entries.Single().Title);
        Assert.Empty(Day(calendar, 2026, 7, 10).Entries);
    }

    [Fact]
    public async Task BuildWeeklyPlanAsync_ForCurrentWeek_ProjectsLessonsFromToday()
    {
        var service = CreateService(
            [
                Lection(1, "First Open", 1m),
                Lection(2, "Second Open", 1m),
            ],
            [],
            new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero));

        var calendar = await service.BuildWeeklyPlanAsync(new DateOnly(2026, 7, 6));

        Assert.Empty(Day(calendar, 2026, 7, 6).Entries);
        Assert.Empty(Day(calendar, 2026, 7, 7).Entries);
        Assert.Equal("First Open", Day(calendar, 2026, 7, 8).Entries.Single().Title);
        Assert.Equal("Second Open", Day(calendar, 2026, 7, 9).Entries.Single().Title);
    }

    [Fact]
    public async Task BuildWeeklyPlanAsync_ForPastWeek_DoesNotProjectLessons()
    {
        var service = CreateService(
            [Lection(1, "Open", 1m)],
            [],
            new DateTimeOffset(2026, 7, 13, 10, 0, 0, TimeSpan.Zero));

        var calendar = await service.BuildWeeklyPlanAsync(new DateOnly(2026, 6, 1));

        Assert.All(calendar.Weeks.SelectMany(w => w.Days), day => Assert.Empty(day.Entries));
    }

    [Fact]
    public async Task BuildWeeklyPlanAsync_IgnoresCompletedAndSkippedLessons()
    {
        var service = CreateService(
            [
                Lection(1, "Finished", 1m),
                Lection(2, "Accepted", 1m),
                Lection(3, "Rated", 1m),
                Lection(4, "Skipped", 1m),
            ],
            [
                Progress(1, LectionStatus.Finished),
                Progress(2, LectionStatus.Accepted),
                Progress(3, LectionStatus.Rated),
                Progress(4, LectionStatus.Skipped),
            ]);

        var calendar = await service.BuildWeeklyPlanAsync(new DateOnly(2026, 7, 6));

        Assert.All(calendar.Weeks.SelectMany(w => w.Days), day => Assert.Empty(day.Entries));
    }

    [Fact]
    public async Task BuildWeeklyPlanAsync_NormalizesWeekendInputToMonday()
    {
        var service = CreateService(
            [Lection(1, "Open", 1m)],
            []);

        var calendar = await service.BuildWeeklyPlanAsync(new DateOnly(2026, 7, 11));

        Assert.Equal(new DateOnly(2026, 7, 6), calendar.From);
        Assert.Equal(6, calendar.Weeks.Count);
        Assert.Equal(5, calendar.HeaderDays.Count);
    }

    private static WeeklyPlanDayViewModel Day(WeeklyPlanViewModel calendar, int year, int month, int day)
    {
        var date = new DateOnly(year, month, day);
        return calendar.Weeks.SelectMany(w => w.Days).Single(d => d.Date == date);
    }

    private static WeeklyPlanService CreateService(
        List<Lection> lections,
        List<LectionProgress> progresses,
        DateTimeOffset? now = null)
    {
        return new WeeklyPlanService(
            new FakeLectionRepository(lections),
            new FakeProgressRepository(progresses),
            new TestCurrentUser("trainee-1"),
            new FixedTimeProvider(now ?? new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.Zero)));
    }

    private static Lection Lection(int number, string title, decimal effort)
    {
        return new Lection
        {
            Id = number,
            Number = number.ToString("00"),
            Title = title,
            Effort = effort,
        };
    }

    private static LectionProgress Progress(int lectionId, LectionStatus status)
    {
        return new LectionProgress
        {
            TraineeId = "trainee-1",
            LectionId = lectionId,
            Status = status,
        };
    }

    private sealed class TestCurrentUser(string traineeId) : ICurrentUserService
    {
        public string TraineeId { get; } = traineeId;
        public string DisplayName => "Test Trainee";
        public string Email => "test@example.org";
        public DateTime TraineeStartDate => new(2026, 1, 1);
        public DateTime TraineeEndDate => new(2026, 12, 31);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow()
        {
            return now.ToUniversalTime();
        }
    }

    private sealed class FakeLectionRepository(List<Lection> lections) : ILectionRepository
    {
        public Task<List<Lection>> GetAllAsync() => Task.FromResult(lections);

        public Task<Lection?> GetByIdAsync(int id)
        {
            return Task.FromResult(lections.FirstOrDefault(l => l.Id == id));
        }

        public Task UpdateAsync(Lection lection)
        {
            var existing = lections.FirstOrDefault(l => l.Id == lection.Id);
            if (existing is not null)
            {
                var index = lections.IndexOf(existing);
                lections[index] = lection;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeProgressRepository(List<LectionProgress> progresses) : ILectionProgressRepository
    {
        public Task<List<LectionProgress>> GetByTraineeIdAsync(string traineeId)
        {
            return Task.FromResult(progresses.Where(p => p.TraineeId == traineeId).ToList());
        }

        public Task<LectionProgress?> GetByLectionIdAsync(int lectionId, string traineeId)
        {
            return Task.FromResult(progresses.FirstOrDefault(p => p.LectionId == lectionId && p.TraineeId == traineeId));
        }

        public Task SaveAsync(LectionProgress progress)
        {
            progresses.RemoveAll(p => p.LectionId == progress.LectionId && p.TraineeId == progress.TraineeId);
            progresses.Add(progress);
            return Task.CompletedTask;
        }

        public Task CreateInitialProgressAsync(string traineeId)
        {
            return Task.CompletedTask;
        }
    }
}
