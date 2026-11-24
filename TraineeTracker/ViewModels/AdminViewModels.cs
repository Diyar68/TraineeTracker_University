using System.ComponentModel.DataAnnotations;

namespace TraineeTracker.ViewModels;

// Autor: Tarik Polat
/// <summary>
/// Ein Benutzer für die Anzeige in der Benutzerliste.
/// </summary>
public class UserListItemViewModel
{
    /// <summary>Id des Benutzers.</summary>
    public string Id { get; set; } = "";

    /// <summary>Voller Name des Benutzers.</summary>
    public string FullName { get; set; } = "";

    /// <summary>E-Mail-Adresse des Benutzers.</summary>
    public string Email { get; set; } = "";

    /// <summary>Rolle des Benutzers.</summary>
    public string Role { get; set; } = "";

    /// <summary>Gibt an, ob das Konto gesperrt ist.</summary>
    public bool IsClosed { get; set; }

    /// <summary>Startdatum, falls der Benutzer ein Trainee ist.</summary>
    public DateTime? TraineeStartDate { get; set; }

    /// <summary>Enddatum, falls der Benutzer ein Trainee ist.</summary>
    public DateTime? TraineeEndDate { get; set; }

    /// <summary>Namen der zugewiesenen Mentoren.</summary>
    public List<string> AssignedMentorNames { get; set; } = new();
}

// Autor: Tarik Polat
/// <summary>
/// Formulardaten zum Anlegen oder Bearbeiten eines Benutzers.
/// </summary>
public class UserFormViewModel
{
    /// <summary>Id des Benutzers, leer bei einem neuen Benutzer.</summary>
    public string Id { get; set; } = "";

    /// <summary>Vorname des Benutzers.</summary>
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    public string FirstName { get; set; } = "";

    /// <summary>Nachname des Benutzers.</summary>
    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    public string LastName { get; set; } = "";

    /// <summary>E-Mail-Adresse des Benutzers.</summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(200)]
    public string Email { get; set; } = "";

    /// <summary>Passwort des Benutzers, optional bei Bearbeitung.</summary>
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    /// <summary>Rolle des Benutzers.</summary>
    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = AppConstants.Roles.Trainee;

    /// <summary>Startdatum, falls die Rolle Trainee ist.</summary>
    public DateTime? TraineeStartDate { get; set; }

    /// <summary>Enddatum, falls die Rolle Trainee ist.</summary>
    public DateTime? TraineeEndDate { get; set; }

    /// <summary>Ids der zugewiesenen Mentoren.</summary>
    public List<string> AssignedMentorIds { get; set; } = new();

    /// <summary>Liste der auswählbaren Mentoren.</summary>
    public List<MentorOptionViewModel> AvailableMentors { get; set; } = new();

    /// <summary>Gibt an, ob es sich um einen neuen Benutzer handelt.</summary>
    public bool IsNew => string.IsNullOrEmpty(Id);
}

// Autor: Tarik Polat
/// <summary>
/// Ein auswählbarer Mentor für die Mentor-Zuordnung.
/// </summary>
public class MentorOptionViewModel
{
    /// <summary>Id des Mentors.</summary>
    public string Id { get; set; } = "";

    /// <summary>Voller Name des Mentors.</summary>
    public string FullName { get; set; } = "";
}

// Autor: Tarik Polat
/// <summary>
/// Daten für die Seite zum Sperren oder Wiedereröffnen eines Kontos.
/// </summary>
public class CloseAccountViewModel
{
    /// <summary>Id des Benutzers.</summary>
    public string Id { get; set; } = "";

    /// <summary>Voller Name des Benutzers.</summary>
    public string FullName { get; set; } = "";

    /// <summary>E-Mail-Adresse des Benutzers.</summary>
    public string Email { get; set; } = "";

    /// <summary>Rolle des Benutzers.</summary>
    public string Role { get; set; } = "";

    /// <summary>Gibt an, ob das Konto aktuell gesperrt ist.</summary>
    public bool IsClosed { get; set; }
}
