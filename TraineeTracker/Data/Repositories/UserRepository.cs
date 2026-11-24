using TraineeTracker.Models;

namespace TraineeTracker.Data.Repositories;

// Autor: Tarik Polat
/// <summary>
/// Kapselt den Datenbankzugriff für Benutzer.
/// </summary>
public class UserRepository
{
    private readonly AppDbContext _context;

    // Autor: Tarik Polat
    /// <summary>
    /// Erstellt das Repository mit dem Datenbankkontext.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Gibt alle Benutzer zurück.
    /// </summary>
    public List<User> GetAll() =>
        _context.Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();

    // Autor: Tarik Polat
    /// <summary>
    /// Gibt alle aktiven Mentoren zurück. Admins zählen nicht als Mentor.
    /// </summary>
    public List<User> GetMentors() =>
        _context.Users
            .Where(u => u.Role == "Mentor" && !u.IsClosed)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToList();

    // Autor: Tarik Polat
    /// <summary>
    /// Gibt alle aktiven Trainees zurück.
    /// </summary>
    public List<User> GetTrainees() =>
        _context.Users.Where(u => u.Role == AppConstants.Roles.Trainee && !u.IsClosed)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();

    // Autor: Tarik Polat
    /// <summary>
    /// Sucht einen Benutzer anhand seiner Id.
    /// </summary>
    /// <param name="id">Id des Benutzers.</param>
    public User? GetById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return _context.Users.FirstOrDefault(u => u.Id == id);
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Sucht einen Benutzer anhand seiner E-Mail-Adresse.
    /// </summary>
    /// <param name="email">E-Mail-Adresse des Benutzers.</param>
    public User? GetByEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return _context.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Prüft, ob eine E-Mail-Adresse bereits verwendet wird.
    /// </summary>
    /// <param name="email">Die zu prüfende E-Mail-Adresse.</param>
    /// <param name="excludingId">Id eines Benutzers, der bei der Prüfung ausgenommen wird.</param>
    public bool EmailExists(string? email, string? excludingId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return _context.Users.Any(u => u.Email.ToLower() == email.ToLower() && u.Id != excludingId);
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Legt einen neuen Benutzer an.
    /// </summary>
    /// <param name="user">Der neue Benutzer.</param>
    public void Add(User user)
    {
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Speichert die Änderungen an einem Benutzer.
    /// </summary>
    /// <param name="user">Der geänderte Benutzer.</param>
    public void Update(User user)
    {
        _context.Users.Update(user);
        _context.SaveChanges();
    }
}
