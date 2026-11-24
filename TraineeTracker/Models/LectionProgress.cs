// Diyar
namespace TraineeTracker.Models
{
    // Models/LectionProgress.cs
    /// <remarks>
    /// Codeownership: Diyar Altintas
    /// </remarks>
    public class LectionProgress
    {
        public int Id { get; set; }
        public required string TraineeId { get; set; }          // ASP.NET Identity User Id
        public int LectionId { get; set; }

        // Status
        public LectionStatus Status { get; set; } = LectionStatus.Open;
        public string? RejectionNote { get; set; } // Abschnitt 7 — Pflichtfeld bei Rejected
        public int? Rating { get; set; } // Abschnitt 7 — Default 3 bei Rated
        public int? IndividualOrderIndex { get; set; } // Sortierung der Lektion pro Trainee

        // Zeitstempel (Abschnitt 1 + 7)
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? RatedAt { get; set; }
        public DateTime? SkippedAt { get; set; }

        public bool KeepsDeprecatedLectionActive =>
            Status is not LectionStatus.Open and not LectionStatus.Skipped;

        // Navigation Properties
        public Lection Lection { get; set; } = null!;
    }
}
