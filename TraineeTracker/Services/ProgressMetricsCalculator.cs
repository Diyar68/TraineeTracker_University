using TraineeTracker.Models;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Services;

/// <summary>Berechnet die sechs Kennzahlen der Fortschrittskontrolle.</summary>
/// <remarks>Codeownership: Florian Pausch</remarks>
public static class ProgressMetricsCalculator
{
    /// <summary>Berechnet die vollständige Fortschrittsstatistik aus Lektionen mit Fortschritt.</summary>
    /// <param name="items">Alle Lektionen des Trainees mit Fortschritt.</param>
    /// <param name="attendedPT">Anwesenheit in PT, oder null bei API-Ausfall.</param>
    /// <param name="isApiAvailable">Ob die makandra-API erreichbar war.</param>
    /// <returns>Die berechneten Kennzahlen als <see cref="DashboardStats"/>.</returns>
    public static DashboardStats Calculate(
        IReadOnlyCollection<LectionWithProgress> items,
        decimal? attendedPT,
        bool isApiAvailable = true)
    {
        var relevantItems = items
            .Where(IsRelevant)
            .ToList();

        var donePT = relevantItems
            .Sum(item => item.Lection.Effort * GetCompletionFactor(item.Status));
        var totalRelevantPT = relevantItems.Sum(item => item.Lection.Effort);
        var openPT = totalRelevantPT - donePT;
        var completed = relevantItems.Count(item =>
            item.Status is LectionStatus.Accepted or LectionStatus.Rated);
        var metrics = CalculateFromTotals(donePT, openPT, attendedPT);

        return new DashboardStats
        {
            TotalCount = relevantItems.Count,
            RatedCount = completed,
            ProgressPercent = relevantItems.Count > 0
                ? (int)Math.Round((decimal)completed / relevantItems.Count * AppConstants.Progress.PercentFactor)
                : 0,
            AttendedPT = attendedPT,
            DonePT = donePT,
            OpenPT = openPT,
            IsApiAvailable = isApiAvailable,
            BufferPT = metrics.BufferPT,
            VelocityPercent = metrics.VelocityPercent,
            ForecastBufferPT = metrics.ForecastBufferPT,
            CountByStatus = relevantItems
                .GroupBy(item => item.Status)
                .ToDictionary(group => group.Key, group => group.Count()),
        };
    }

    // <summary>Berechnet Puffer, Geschwindigkeit und Vorhersage aus PT-Summen.</summary>
    /// <param name="donePT">Bereits gewichtete Summe "Geschafft" in PT.</param>
    /// <param name="openPT">Summe "Offen" in PT.</param>
    /// <param name="attendedPT">Anwesenheit in PT, oder null wenn nicht verfügbar.</param>
    /// <returns>Puffer, Geschwindigkeit und Vorhersage als <see cref="ProgressMetricValues"/>.</returns>
    public static ProgressMetricValues CalculateFromTotals(
        decimal donePT,
        decimal openPT,
        decimal? attendedPT)
    {
        if (attendedPT is null)
            return new ProgressMetricValues(null, null, null);

        var attended = attendedPT.Value;
        var bufferPT = donePT - attended;

        if (attended <= 0)
            return new ProgressMetricValues(bufferPT, null, null);

        var velocityPercent = donePT / attended * AppConstants.Progress.PercentFactor;
        if (velocityPercent <= 0)
            return new ProgressMetricValues(bufferPT, velocityPercent, null);

        var velocityFactor = velocityPercent / AppConstants.Progress.PercentFactor;
        var futureAttendancePT = openPT / velocityFactor;
        var forecastBufferPT = donePT + openPT - attended - futureAttendancePT;

        return new ProgressMetricValues(bufferPT, velocityPercent, forecastBufferPT);
    }

    /// <summary>Prüft, ob eine Lektion in die Berechnung einfließen soll.</summary>
    /// <param name="item">Die zu prüfende Lektion mit Fortschritt.</param>
    /// <returns>True, wenn relevant.</returns>
    private static bool IsRelevant(LectionWithProgress item)
    {
        return item.Status != LectionStatus.Skipped
            && (item.Lection.IsActive || item.Status != LectionStatus.Open);
    }

    /// <summary>Liefert den Gewichtungsfaktor je nach Lektionsstatus.</summary>
    /// <param name="status">Der aktuelle Status der Lektion.</param>
    /// <returns>Faktor zwischen 0 und 1.</returns>
    private static decimal GetCompletionFactor(LectionStatus status) => status switch
    {
        LectionStatus.Finished => AppConstants.Progress.FinishedCompletionFactor,
        LectionStatus.Rejected => AppConstants.Progress.RejectedCompletionFactor,
        LectionStatus.Accepted => AppConstants.Progress.CompleteCompletionFactor,
        LectionStatus.Rated => AppConstants.Progress.CompleteCompletionFactor,
        _ => 0m,
    };
}

public readonly record struct ProgressMetricValues(
    decimal? BufferPT,
    decimal? VelocityPercent,
    decimal? ForecastBufferPT);
