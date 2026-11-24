using Microsoft.EntityFrameworkCore;
using TraineeTracker.Data;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using TraineeTracker.Services;

namespace TraineeTracker.UnitTests;

public class CurriculumImportSyncTests
{
    [Fact]
    public async Task ImportAsync_CreatesLectionWithGeneratedInternalIdAndExternalId()
    {
        await using var context = CreateContext();
        var repository = new DatabaseLectionRepository(context);

        await repository.ImportAsync(1,
        [
            Imported(122, "01", "100 CSS machen", 1.5m, "https://cards.example/css", 0),
        ]);

        var lection = await context.Lections.SingleAsync();
        Assert.NotEqual(122, lection.Id);
        Assert.Equal(122, lection.ExternalId);
        Assert.Equal("100 CSS machen", lection.Title);
        Assert.Equal(0, lection.OrderIndex);
    }

    [Fact]
    public async Task ImportAsync_ReimportSameExternalId_UpdatesExistingLectionAndKeepsInternalId()
    {
        await using var context = CreateContext();
        var repository = new DatabaseLectionRepository(context);

        await repository.ImportAsync(1,
        [
            Imported(122, "01", "100 CSS machen", 1.5m, "https://cards.example/css", 0),
        ]);
        var originalId = await context.Lections.Select(l => l.Id).SingleAsync();

        await repository.ImportAsync(1,
        [
            Imported(122, "03", "CSS sauber machen", 2.25m, "https://cards.example/css-new", 2),
        ]);

        var lection = await context.Lections.SingleAsync();
        Assert.Equal(originalId, lection.Id);
        Assert.Equal(122, lection.ExternalId);
        Assert.Equal("CSS sauber machen", lection.Title);
        Assert.Equal(2.25m, lection.Effort);
        Assert.Equal("https://cards.example/css-new", lection.ExternalLink);
        Assert.Equal(2, lection.OrderIndex);
    }

    [Fact]
    public async Task ImportAsync_ReimportSameExternalId_KeepsExistingProgressConnectedToInternalLection()
    {
        await using var context = CreateContext();
        var repository = new DatabaseLectionRepository(context);

        await repository.ImportAsync(1,
        [
            Imported(122, "01", "100 CSS machen", 1.5m, "https://cards.example/css", 0),
        ]);
        var internalId = await context.Lections.Select(l => l.Id).SingleAsync();
        context.LectionProgresses.Add(new LectionProgress
        {
            TraineeId = "trainee-1",
            LectionId = internalId,
            Status = LectionStatus.Started,
        });
        await context.SaveChangesAsync();

        await repository.ImportAsync(1,
        [
            Imported(122, "01", "100 CSS machen aktualisiert", 1.75m, "https://cards.example/css", 0),
        ]);

        var progress = await context.LectionProgresses.SingleAsync();
        Assert.Equal(internalId, progress.LectionId);
        Assert.Equal(internalId, await context.Lections.Select(l => l.Id).SingleAsync());
    }

    [Fact]
    public async Task ImportAsync_RejectsDuplicateExternalIds()
    {
        await using var context = CreateContext();
        var repository = new DatabaseLectionRepository(context);

        await Assert.ThrowsAsync<InvalidDataException>(() => repository.ImportAsync(1,
        [
            Imported(122, "01", "CSS", 1m, "https://cards.example/css", 0),
            Imported(122, "02", "Rails", 1m, "https://cards.example/rails", 1),
        ]));
    }

    [Fact]
    public async Task ImportAsync_AllowsDifferentExternalIdsWithSameTitleAndDisplayNumber()
    {
        await using var context = CreateContext();
        var repository = new DatabaseLectionRepository(context);

        await repository.ImportAsync(1,
        [
            Imported(122, "01", "100 CSS machen", 1m, "https://cards.example/css-a", 0),
            Imported(123, "01", "100 CSS machen", 1m, "https://cards.example/css-b", 1),
        ]);

        var lections = await context.Lections.OrderBy(l => l.ExternalId).ToListAsync();
        Assert.Equal([122, 123], lections.Select(l => l.ExternalId));
        Assert.Equal(2, lections.Select(l => l.Id).Distinct().Count());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ImportedLection Imported(
        long externalId,
        string number,
        string title,
        decimal effort,
        string externalLink,
        int orderIndex)
    {
        return new ImportedLection(
            externalId,
            number,
            title,
            effort,
            externalLink,
            orderIndex,
            false);
    }
}
