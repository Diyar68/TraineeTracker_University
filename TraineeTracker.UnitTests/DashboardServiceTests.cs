using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using TraineeTracker.Services;

namespace TraineeTracker.UnitTests;

/// <remarks>
/// </remarks>
public class DashboardServiceTests
{
    [Fact]
    public async Task BuildDashboardAsync_CombinesLectionsWithProgressForCurrentTrainee()
    {
        var service = new DashboardService(
            new FakeLectionRepository(
            [
                Lection(1, "Open", 1m),
                Lection(2, "Accepted", 2m),
                Lection(3, "Skipped", 3m),
            ]),
            new FakeProgressRepository(
            [
                Progress("current-trainee", 2, LectionStatus.Accepted),
                Progress("current-trainee", 3, LectionStatus.Skipped),
                Progress("other-trainee", 1, LectionStatus.Rated),
            ]),
            new DashboardGroupingService(),
            new ProgressStatsService(),
            new TestCurrentUser("current-trainee"),
            new FakeAttendanceService(6m));

        var dashboard = await service.BuildDashboardAsync();

        Assert.Equal(2, dashboard.Stats.TotalCount);
        Assert.Equal(1, dashboard.Stats.RatedCount);
        Assert.Equal(50, dashboard.Stats.ProgressPercent);
        Assert.Equal(6m, dashboard.Stats.AttendedPT);
        Assert.Contains(dashboard.Groups, g => g.Label == "Als Nächstes" && g.Items.Single().Lection.Id == 1);
        Assert.Contains(dashboard.Groups, g => g.Label == "Feedback offen" && g.Items.Single().Lection.Id == 2);
        Assert.Contains(dashboard.Groups, g => g.Label == "Übersprungen" && g.Items.Single().Lection.Id == 3);
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

    private static LectionProgress Progress(string traineeId, int lectionId, LectionStatus status)
    {
        return new LectionProgress
        {
            TraineeId = traineeId,
            LectionId = lectionId,
            Status = status,
        };
    }

    private sealed class TestCurrentUser(string traineeId) : ICurrentUserService
    {
        public string TraineeId { get; } = traineeId;
        public string DisplayName => "Test Trainee";
        public string Email => "test@portfolio.example";
        public DateTime TraineeStartDate => new(2026, 1, 1);
        public DateTime TraineeEndDate => new(2026, 12, 31);
    }

    private sealed class FakeAttendanceService(decimal? attendedPT) : IAttendanceService
    {
        public Task<AttendanceResult> GetAttendedPersonDaysAsync(
            string traineeId,
            string email,
            DateTime startDate,
            DateTime endDate)
        {
            return Task.FromResult(new AttendanceResult
            {
                AttendedPT = attendedPT,
                IsApiAvailable = attendedPT is not null,
            });
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
