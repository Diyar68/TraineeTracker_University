namespace TraineeTracker.Models
{
    /// <summary>
    /// Feedback eines Trainees zu einer abgeschlossenen Lektion.
    /// </summary>
    public class Feedback
    {
        /// <summary>Id des Feedbacks.</summary>
        public int Id { get; set; }

        /// <summary>Id des zugehörigen Lernfortschritts.</summary>
        public int LectionProgressId { get; set; }

        /// <summary>Schwierigkeit der Lektion: 1 = leicht, 2 = mittel, 3 = schwer.</summary>
        public int DifficultyRating { get; set; }

        /// <summary>Vorkenntnisse des Trainees zur Lektion.</summary>
        public string PriorKnowledge { get; set; } = "";

        /// <summary>Tatsächlicher Aufwand in Personentagen.</summary>
        public decimal ActualEffortDays { get; set; }

        /// <summary>Optionale Anmerkung des Trainees.</summary>
        public string? Note { get; set; }

        /// <summary>Zeitpunkt, zu dem das Feedback angelegt wurde.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Der zugehörige Lernfortschritt.</summary>
        public LectionProgress Progress { get; set; } = null!;
    }
}
