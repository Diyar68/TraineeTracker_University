namespace TraineeTracker.Models
{
    // Models/Lection.cs
    /// <remarks>
    /// </remarks>
    public class Lection
    {
        public int Id { get; set; }
        public long ExternalId { get; set; }
        public required string Number { get; set; }       // "01", "02", ... (Abschnitt 1)
        public required string Title { get; set; }        // Abschnitt 1
        public decimal Effort { get; set; }        // in PT (Abschnitt 1)
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeprecated
        {
            get => !IsActive;
            set => IsActive = !value;
        }

        public string? ExternalLink { get; set; }        // Link zum Card-Deck (Abschnitt 1)
        // Navigation Properties
        public ICollection<LectionProgress> Progresses { get; set; } = new List<LectionProgress>();
    }
}
