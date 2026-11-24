using TraineeTracker.Models;
using TraineeTracker.ViewModels;

namespace TraineeTracker.UnitTests;

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class TransitionResultViewModelTests
{
    [Fact]
    public void From_IncludesRenderedCardHtml()
    {
        var cardHtml = """
            <div class="lesson-card rejected"
                 data-testid="lesson-card"
                 data-id="42"
                 data-status="rejected"
                 data-group-label="Handlungsbedarf"
                 onclick="openLesson(42)">
                <span class="badge-label">Abgelehnt</span>
                <div class="rejection-note"><span>Bitte Tests ergänzen</span></div>
            </div>
            """;
        var modalContentHtml = """
            <div id="modal-title">Testlektion</div>
            <div id="modal-actions">
                <button data-testid="transition-rejected-to-finished">Erneut einreichen</button>
            </div>
            """;

        var result = TransitionResultViewModel.From(
            Lection(),
            Progress(LectionStatus.Rejected),
            LectionStatus.Finished,
            Stats(),
            cardHtml,
            modalContentHtml);

        Assert.Equal(cardHtml, result.CardHtml);
        Assert.Contains("data-status=\"rejected\"", result.CardHtml);
        Assert.Contains("Bitte Tests ergänzen", result.CardHtml);
        Assert.Contains("data-id=\"42\"", result.CardHtml);
        Assert.Contains("onclick=\"openLesson(42)\"", result.CardHtml);
        Assert.Equal(modalContentHtml, result.ModalContentHtml);
        Assert.Contains("modal-title", result.ModalContentHtml);
        Assert.Contains("transition-rejected-to-finished", result.ModalContentHtml);
    }

    private static Lection Lection()
    {
        return new Lection
        {
            Id = 42,
            ExternalId = 142,
            Number = "01",
            Title = "Testlektion",
            Effort = 1m,
        };
    }

    private static LectionProgress Progress(LectionStatus status)
    {
        return new LectionProgress
        {
            TraineeId = "trainee-1",
            LectionId = 42,
            Status = status,
            RejectionNote = "Bitte Tests ergänzen",
        };
    }

    private static DashboardStats Stats()
    {
        return new DashboardStats
        {
            TotalCount = 1,
            RatedCount = 0,
            ProgressPercent = 0,
            DonePT = 0m,
            OpenPT = 1m,
            IsApiAvailable = false,
            CountByStatus = new Dictionary<LectionStatus, int>
            {
                [LectionStatus.Rejected] = 1,
            },
        };
    }
}
