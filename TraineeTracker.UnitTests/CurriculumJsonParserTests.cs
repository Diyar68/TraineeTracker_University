using System.Text;
using TraineeTracker.Services;

namespace TraineeTracker.UnitTests;

public class CurriculumJsonParserTests
{
    [Fact]
    public async Task ParseAsync_UsesJsonPositionAsDisplayNumber_NotLeadingTitleNumber()
    {
        var parser = new CurriculumJsonParser();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            [
              { "id": 122, "title": "100 CSS machen", "estimate": 1.5, "url": "https://cards.example/css" },
              { "id": 130, "title": "Rails Routing", "estimate": 1.0, "url": "https://cards.example/routing" }
            ]
            """));

        var lections = await parser.ParseAsync(stream);

        Assert.Equal(122, lections[0].ExternalId);
        Assert.Equal("100 CSS machen", lections[0].Title);
        Assert.Equal("01", lections[0].Number);
        Assert.Equal(0, lections[0].OrderIndex);

        Assert.Equal(130, lections[1].ExternalId);
        Assert.Equal("02", lections[1].Number);
        Assert.Equal(1, lections[1].OrderIndex);
    }

    [Fact]
    public async Task ParseAsync_RejectsDuplicateExternalIds()
    {
        var parser = new CurriculumJsonParser();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            [
              { "id": 122, "title": "CSS", "estimate": 1.0, "url": "https://cards.example/css" },
              { "id": 122, "title": "Rails", "estimate": 1.0, "url": "https://cards.example/rails" }
            ]
            """));

        await Assert.ThrowsAsync<InvalidDataException>(() => parser.ParseAsync(stream));
    }
}
