using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraineeTracker.Controllers;
using TraineeTracker.Data;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using TraineeTracker.Services;


namespace TraineeTracker.UnitTests;

public class TeachingplanControllerTests
{
    [Fact]
    public async Task SaveOrder_UpdatesIndividualOrderIndex()
    {
        var traineeId = "trainee-1";
        var context = CreateInMemoryContext();

        var lection = new Lection
        {
            Number = "L001",
            Title = "HTML",
            Effort = 3m,
            OrderIndex = 0
        };

        context.Lections.Add(lection);
        context.LectionProgresses.Add(new LectionProgress
        {
            TraineeId = traineeId,
            LectionId = lection.Id,
            Status = LectionStatus.Open
        });
        await context.SaveChangesAsync();

        var controller = new TeachingplanController(
            new DatabaseLectionProgressRepository(context),
            new FakeCurrentUserService(traineeId));

        var result = await controller.SaveOrder([
            new TeachingplanController.OrderItemDto
            {
                lessonId = lection.Id,
                individualOrderIndex = 5
            }
        ]);

        Assert.IsType<OkResult>(result);

        var saved = await context.LectionProgresses.FirstAsync(p => p.TraineeId == traineeId);
        Assert.Equal(5, saved.IndividualOrderIndex);
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class FakeCurrentUserService(string traineeId) : ICurrentUserService
    {
        public string TraineeId => traineeId;
        public string DisplayName => "Test Trainee";
        public string Email => "trainee@portfolio.example";
        public DateTime TraineeStartDate => DateTime.Today;
        public DateTime TraineeEndDate => DateTime.Today.AddYears(1);
    }
}