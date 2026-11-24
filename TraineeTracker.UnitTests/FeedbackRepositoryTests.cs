using Microsoft.EntityFrameworkCore;
using TraineeTracker.Data;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using Xunit;

namespace TraineeTracker.UnitTests;

/// <summary>
/// Tests für das Feedback-Repository.
/// </summary>
public class FeedbackRepositoryTests
{
    /// <summary>
    /// Prüft, dass das Löschen eines Feedbacks den Fortschritt von Bewertet
    /// zurück auf Akzeptiert setzt.
    /// </summary>
    [Fact]
    public void Delete_RevertsToAccepted()
    {
        // Arrange
        using var context = CreateContext();

        context.Lections.Add(new Lection
        {
            Id = 1,
            Number = "01",
            Title = "Git Grundlagen",
            Effort = 1,
        });

        context.LectionProgresses.Add(new LectionProgress
        {
            Id = 1,
            TraineeId = "trainee-1",
            LectionId = 1,
            Status = LectionStatus.Rated,
            RatedAt = DateTime.Now,
        });

        context.Feedbacks.Add(new Feedback
        {
            Id = 1,
            LectionProgressId = 1,
            DifficultyRating = 2,
            PriorKnowledge = "etwas Erfahrung",
            ActualEffortDays = 1.5m,
            CreatedAt = DateTime.Now,
        });

        context.SaveChanges();

        var repository = new FeedbackRepository(context);

        // Act
        var reverted = repository.Delete(1);

        // Assert
        Assert.NotNull(reverted);
        Assert.Equal(LectionStatus.Accepted, reverted!.Progress.Status);
        Assert.Null(reverted.Progress.RatedAt);
        Assert.Empty(context.Feedbacks);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
