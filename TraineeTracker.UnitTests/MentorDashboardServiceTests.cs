using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TraineeTracker.Data;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using TraineeTracker.Services;
using TraineeTracker.ViewModels;

namespace TraineeTracker.UnitTests;

public class MentorDashboardServiceTests
{
    [Fact]
    public async Task BuildMentorDashboardAsync_UsesCentralAttentionCountForTraineeCards()
    {
        var lections = new List<Lection>
        {
            Lection(1, "Started", 1m),
            Lection(2, "Rejected", 10m),
            Lection(3, "Accepted", 2m),
            Lection(4, "Finished", 5m),
        };
        var progress = new List<LectionProgress>
        {
            Progress("trainee-1", 1, LectionStatus.Started),
            Progress("trainee-1", 2, LectionStatus.Rejected),
            Progress("trainee-1", 3, LectionStatus.Accepted),
            Progress("trainee-1", 4, LectionStatus.Finished),
        };
        await using var context = CreateContext();
        SeedTrainee(context);
        var service = CreateService(context, lections, progress, attendedPT: 10m);

        var dashboard = await service.BuildMentorDashboardAsync();

        var card = Assert.Single(dashboard.Trainees);
        Assert.Equal(2, card.AttentionCount);
    }

    [Fact]
    public async Task BuildMentorDashboardAsync_UsesWeightedGeschafftPercentageForTraineeCards()
    {
        var lections = new List<Lection>
        {
            Lection(1, "Finished", 10m),
            Lection(2, "Open", 10m),
        };
        var progress = new List<LectionProgress>
        {
            Progress("trainee-1", 1, LectionStatus.Finished),
            Progress("trainee-1", 2, LectionStatus.Open),
        };
        await using var context = CreateContext();
        SeedTrainee(context);
        var service = CreateService(context, lections, progress, attendedPT: 10m);

        var dashboard = await service.BuildMentorDashboardAsync();

        var card = Assert.Single(dashboard.Trainees);
        Assert.Equal(35, card.ProgressPercent);
    }

    [Fact]
    public void ProgressStatsService_CountsFinishedAsReview()
    {
        var lections = new List<Lection>
        {
            Lection(1, "Finished", 5m),
            Lection(2, "Accepted", 2m),
        };
        var progress = new List<LectionProgress>
        {
            Progress("trainee-1", 1, LectionStatus.Finished),
            Progress("trainee-1", 2, LectionStatus.Accepted),
        };
        var items = lections
            .Select(l => new LectionWithProgress(l, progress.FirstOrDefault(p => p.LectionId == l.Id)))
            .ToList();

        var stats = new ProgressStatsService().BuildStats(items, attendedPT: 10m);

        Assert.Equal(1, stats.ReviewCount);
        Assert.Equal(0, stats.AttentionCount);
    }

    [Fact]
    public async Task BuildMentorDashboardAsync_PopulatesLessonsForOverview()
    {
        var lections = new List<Lection>
        {
            Lection(1, "Started", 1m),
            Lection(2, "Accepted", 2m),
        };
        var progress = new List<LectionProgress>();
        await using var context = CreateContext();
        SeedTrainee(context);
        var service = CreateService(context, lections, progress, attendedPT: 10m);

        var dashboard = await service.BuildMentorDashboardAsync();

        Assert.Equal(2, dashboard.Lessons.Count);
        Assert.Contains(dashboard.Lessons, lesson => lesson.Title == "Started");
    }

    [Fact]
    public async Task BuildMentorDashboardAsync_UsesCentralBufferDisplayForTraineeCards()
    {
        var lections = new List<Lection>
        {
            Lection(1, "Rejected", 10m),
            Lection(2, "Accepted", 2m),
            Lection(3, "Finished", 5m),
            Lection(4, "Started", 1m),
        };
        var progress = new List<LectionProgress>
        {
            Progress("trainee-1", 1, LectionStatus.Rejected),
            Progress("trainee-1", 2, LectionStatus.Accepted),
            Progress("trainee-1", 3, LectionStatus.Finished),
            Progress("trainee-1", 4, LectionStatus.Started),
        };
        await using var context = CreateContext();
        SeedTrainee(context);
        var service = CreateService(context, lections, progress, attendedPT: 10m);

        var dashboard = await service.BuildMentorDashboardAsync();

        var card = Assert.Single(dashboard.Trainees);
        Assert.Equal("+3,5 PT", card.BufferText);
        Assert.False(card.BufferIsNegative);
    }

    [Fact]
    public async Task BuildMentorDashboardAsync_ShowsUnavailableBufferWhenAttendanceIsUnavailable()
    {
        var lections = new List<Lection>
        {
            Lection(1, "Accepted", 2m),
        };
        var progress = new List<LectionProgress>
        {
            Progress("trainee-1", 1, LectionStatus.Accepted),
        };
        await using var context = CreateContext();
        SeedTrainee(context);
        var service = CreateService(context, lections, progress, attendedPT: null);

        var dashboard = await service.BuildMentorDashboardAsync();

        var card = Assert.Single(dashboard.Trainees);
        Assert.Equal("–", card.BufferText);
        Assert.False(card.BufferIsNegative);
    }

    private static MentorDashboardService CreateService(
        AppDbContext context,
        List<Lection> lections,
        List<LectionProgress> progress,
        decimal? attendedPT)
    {
        var httpContextAccessor = AdminHttpContextAccessor();
        var lectionRepository = new FakeLectionRepository(lections);
        var progressRepository = new FakeProgressRepository(progress);
        var mentorAssignmentRepository = new MentorAssignmentRepository(context);
        var feedbackRepository = new FeedbackRepository(context);
        var currentUser = new TestCurrentUser("trainee-1");
        var feedbackService = new DemoFeedbackService(
            lectionRepository,
            progressRepository,
            feedbackRepository,
            mentorAssignmentRepository,
            currentUser,
            httpContextAccessor);

        return new MentorDashboardService(
            new UserRepository(context),
            mentorAssignmentRepository,
            lectionRepository,
            progressRepository,
            new ProgressStatsService(),
            new FakeAttendanceService(attendedPT),
            feedbackService,
            httpContextAccessor);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedTrainee(AppDbContext context)
    {
        context.Users.Add(new User
        {
            Id = "trainee-1",
            FirstName = "Test",
            LastName = "Trainee",
            Email = "test.trainee@example.org",
            PasswordHash = "hash",
            Role = AppConstants.Roles.Trainee,
            TraineeStartDate = new DateTime(2026, 1, 1),
            TraineeEndDate = new DateTime(2026, 12, 31),
        });
        context.SaveChanges();
    }

    private static IHttpContextAccessor AdminHttpContextAccessor()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "admin-1"),
            new(ClaimTypes.Name, "Admin User"),
            new(ClaimTypes.Role, AppConstants.Roles.Admin),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");

        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };
    }

    private static Lection Lection(int id, string title, decimal effort)
    {
        return new Lection
        {
            Id = id,
            ExternalId = id,
            Number = id.ToString("00"),
            Title = title,
            Effort = effort,
            OrderIndex = id,
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
        public string Email => "test.trainee@example.org";
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
