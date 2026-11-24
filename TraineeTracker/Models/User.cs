// Models/User.cs
namespace TraineeTracker.Models
{
    /// <summary>
    /// Registrierter Benutzer der Anwendung. Deckt alle drei Rollen ab
    /// (Trainee, Mentor, Admin) — die Rolle steht im Feld <see cref="Role"/>.
    /// </summary>
    public class User
    {
        public required string Id { get; set; }          // ASP.NET Identity User Id (string, wie bei LectionProgress etc.)
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string Role { get; set; }         // "Trainee" | "Mentor" | "Admin"
        public bool IsClosed { get; set; }

        // Nur für Role == "Trainee" gesetzt.
        public DateTime? TraineeStartDate { get; set; }
        public DateTime? TraineeEndDate { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
