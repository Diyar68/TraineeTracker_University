using TraineeTracker.Models;

namespace TraineeTracker.ViewModels
{
    public class DashboardStats
    {
        public int TotalCount { get; init; }
        public int RatedCount { get; init; }      // Fortschrittswert: accepted + rated
        public int ProgressPercent { get; init; }  // 0–100

        public decimal? AttendedPT { get; init; }    // null, wenn Zeiterfassung nicht berechenbar ist
        public decimal DonePT { get; init; }    // gewichteter Fortschritt nach Status
        public decimal OpenPT { get; init; }    // relevanter Lehrplan minus DonePT
        public bool IsApiAvailable { get; init; } = true;
        public decimal? BufferPT { get; init; }
        public decimal? VelocityPercent { get; init; }
        public decimal? ForecastBufferPT { get; init; }

        public Dictionary<LectionStatus, int> CountByStatus { get; init; } = new();

        public string HeaderBufferText => StatisticsDisplayViewModel.From(this).BufferText;
        public string HeaderBufferClass => StatisticsDisplayViewModel.From(this).BufferToneClass;
        public int AttentionCount =>
            CountByStatus.GetValueOrDefault(LectionStatus.Started, 0)
            + CountByStatus.GetValueOrDefault(LectionStatus.Rejected, 0);
        public int ReviewCount => CountByStatus.GetValueOrDefault(LectionStatus.Finished, 0);
        public int RejectedCount => CountByStatus.GetValueOrDefault(LectionStatus.Rejected, 0);
        public string HeaderSummaryText =>
            $"{AttentionCount} Lektionen brauchen Aufmerksamkeit, davon {ReviewCount} warten auf Review"
            + (RejectedCount > 0 ? $" und {RejectedCount} auf Überarbeitung." : ".");
    }
}
