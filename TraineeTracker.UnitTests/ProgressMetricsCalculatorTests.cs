using TraineeTracker.Models;
using TraineeTracker.Services;
using TraineeTracker.ViewModels;

namespace TraineeTracker.UnitTests;

/// <summary>Tests für die Kennzahlen-Berechnung.</summary>
/// <remarks>Codeownership: Florian Pausch</remarks>
public class ProgressMetricsCalculatorTests
{
    /// <summary>Prüft die Gewichtung je Lektionsstatus in DonePT/OpenPT.</summary>
    /// <param name="status">Der zu prüfende Lektionsstatus.</param>
    /// <param name="expectedDonePT">Erwarteter DonePT-Wert bei 10 PT Aufwand.</param>
    [Theory]
    [InlineData(LectionStatus.Open, 0)]
    [InlineData(LectionStatus.Started, 0)]
    [InlineData(LectionStatus.Finished, 7)]
    [InlineData(LectionStatus.Rejected, 8)]
    [InlineData(LectionStatus.Accepted, 10)]
    [InlineData(LectionStatus.Rated, 10)]
    public void Calculate_AppliesExpectedStatusWeight(LectionStatus status, decimal expectedDonePT)
    {
        var result = ProgressMetricsCalculator.Calculate(
            [Item(10m, status)],
            attendedPT: 5m);

        Assert.Equal(expectedDonePT, result.DonePT);
        Assert.Equal(10m - expectedDonePT, result.OpenPT);
    }

    /// <summary>Übersprungene Lektionen fließen nicht in die Statistik ein.</summary>
    [Fact]
    public void Calculate_ExcludesSkippedCompletely()
    {
        var result = ProgressMetricsCalculator.Calculate(
            [Item(10m, LectionStatus.Skipped)],
            attendedPT: 5m);

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0m, result.DonePT);
        Assert.Equal(0m, result.OpenPT);
    }

    /// <summary>Inaktive, noch nicht begonnene Lektionen werden ausgeschlossen.</summary>
    [Fact]
    public void Calculate_ExcludesInactiveOpenLection()
    {
        var result = ProgressMetricsCalculator.Calculate(
            [Item(10m, LectionStatus.Open, isActive: false)],
            attendedPT: 5m);

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0m, result.DonePT);
        Assert.Equal(0m, result.OpenPT);
    }

    /// <summary>Inaktive, aber bereits begonnene Lektionen bleiben enthalten.</summary>
    [Fact]
    public void Calculate_IncludesInactiveLectionThatWasStarted()
    {
        var result = ProgressMetricsCalculator.Calculate(
            [Item(10m, LectionStatus.Finished, isActive: false)],
            attendedPT: 5m);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(7m, result.DonePT);
        Assert.Equal(3m, result.OpenPT);
    }

    /// <summary>Dezimale Aufwände werden ohne Rundungsfehler übernommen.</summary>
    [Fact]
    public void Calculate_PreservesDecimalEffort()
    {
        var result = ProgressMetricsCalculator.Calculate(
            [Item(0.5m, LectionStatus.Finished)],
            attendedPT: 0.25m);

        Assert.Equal(0.35m, result.DonePT);
        Assert.Equal(0.15m, result.OpenPT);
    }

    /// <summary>Puffer, Geschwindigkeit und Vorhersage im Normalfall.</summary>
    [Fact]
    public void CalculateFromTotals_CalculatesBufferVelocityAndForecast()
    {
        var result = ProgressMetricsCalculator.CalculateFromTotals(
            donePT: 12m,
            openPT: 8m,
            attendedPT: 10m);

        Assert.Equal(2m, result.BufferPT);
        Assert.Equal(120m, result.VelocityPercent);
        Assert.Equal(10m / 3m, result.ForecastBufferPT);
    }

    /// <summary>Geschwindigkeit wird vor der Vorhersage nicht gerundet.</summary>
    [Fact]
    public void CalculateFromTotals_DoesNotRoundVelocityBeforeForecast()
    {
        var result = ProgressMetricsCalculator.CalculateFromTotals(
            donePT: 1m,
            openPT: 2m,
            attendedPT: 3m);

        Assert.InRange(result.VelocityPercent!.Value, 33.33333333333333333333333332m, 33.33333333333333333333333334m);
        Assert.InRange(result.ForecastBufferPT!.Value, -6.000000000000000000000000001m, -5.999999999999999999999999999m);
    }

    /// <summary>Ohne Anwesenheitsdaten sind alle drei Kennzahlen null.</summary>
    [Fact]
    public void CalculateFromTotals_WithoutAttendance_ReturnsNotCalculableValues()
    {
        var result = ProgressMetricsCalculator.CalculateFromTotals(
            donePT: 2m,
            openPT: 8m,
            attendedPT: null);

        Assert.Null(result.BufferPT);
        Assert.Null(result.VelocityPercent);
        Assert.Null(result.ForecastBufferPT);
    }

    /// <summary>Bei 0 % Geschwindigkeit wird keine Vorhersage berechnet.</summary>
    [Fact]
    public void CalculateFromTotals_WithoutProgress_ReturnsNoForecast()
    {
        var result = ProgressMetricsCalculator.CalculateFromTotals(
            donePT: 0m,
            openPT: 8m,
            attendedPT: 2m);

        Assert.Equal(-2m, result.BufferPT);
        Assert.Equal(0m, result.VelocityPercent);
        Assert.Null(result.ForecastBufferPT);
    }

    /// <summary>Erstellt eine Test-Lektion mit Fortschritt für die Testfälle.</summary>
    /// <param name="effort">Der Aufwand der Lektion in PT.</param>
    /// <param name="status">Der Bearbeitungsstatus.</param>
    /// <param name="isActive">Ob die Lektion aktiv ist.</param>
    /// <returns>Eine Lektion mit passendem Fortschritt.</returns>
    private static LectionWithProgress Item(
        decimal effort,
        LectionStatus status,
        bool isActive = true)
    {
        var lection = new Lection
        {
            Id = 1,
            Number = "01",
            Title = "Test",
            Effort = effort,
            IsActive = isActive,
        };

        var progress = status == LectionStatus.Open
            ? null
            : new LectionProgress
            {
                TraineeId = "test-trainee",
                LectionId = lection.Id,
                Status = status,
            };

        return new LectionWithProgress(lection, progress);
    }
}