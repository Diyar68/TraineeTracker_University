// Models/TraineeMentorAssignment.cs
namespace TraineeTracker.Models
{
    /// <summary>
    /// Ordnet einen Trainee einem Mentor zu (Verknüpfungstabelle).
    /// </summary>
    public class TraineeMentorAssignment
    {
        public required string TraineeId { get; set; }
        public required string MentorId { get; set; }
    }
}
