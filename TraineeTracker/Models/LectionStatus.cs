// Diyar
using System.ComponentModel.DataAnnotations;
namespace TraineeTracker.Models
{
    /// <remarks>
    /// Codeownership: Diyar Altintas
    /// </remarks>
    public enum LectionStatus
    {
        [Display(Name = "Offen")] Open,
        [Display(Name = "Gestartet")] Started,
        [Display(Name = "Eingereicht")] Finished,
        [Display(Name = "Akzeptiert")] Accepted,
        [Display(Name = "Abgelehnt")] Rejected,
        [Display(Name = "Bewertet")] Rated,
        [Display(Name = "Übersprungen")] Skipped,
    }
}
