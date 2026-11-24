using Microsoft.AspNetCore.Mvc;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Filters;
using TraineeTracker.Helpers;
using TraineeTracker.Models;
using TraineeTracker.Services;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Controllers;

// Autor: Tarik Polat
/// <summary>
/// Steuert das Admin-Cockpit zur Benutzerverwaltung.
/// </summary>
[RequireDemoRole(AppConstants.RoleValues.Admin)]
public class AdminController : Controller
{
    private readonly UserRepository _userRepository;
    private readonly MentorAssignmentRepository _mentorAssignmentRepository;
    private readonly ILectionProgressRepository _progressRepository;

    // Autor: Tarik Polat
    /// <summary>
    /// Erstellt den Controller mit den benötigten Repositories.
    /// </summary>
    /// <param name="userRepository">Repository für Benutzer.</param>
    /// <param name="mentorAssignmentRepository">Repository für Mentor-Zuordnungen.</param>
    /// <param name="progressRepository">Repository für den Lernfortschritt.</param>
    public AdminController(
        UserRepository userRepository,
        MentorAssignmentRepository mentorAssignmentRepository,
        ILectionProgressRepository progressRepository)
    {
        _userRepository = userRepository;
        _mentorAssignmentRepository = mentorAssignmentRepository;
        _progressRepository = progressRepository;
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Zeigt die Liste aller Benutzer.
    /// </summary>
    public IActionResult Index()
    {
        SetHeader("Benutzerverwaltung");

        var mentorNamesByTraineeId = _mentorAssignmentRepository.GetMentorNamesByTraineeId();

        var users = _userRepository.GetAll()
            .Select(u => ToListItem(u, mentorNamesByTraineeId))
            .ToList();

        return View(users);
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Zeigt das Formular zum Anlegen eines neuen Benutzers.
    /// </summary>
    public IActionResult Create()
    {
        SetHeader("Benutzer anlegen");
        return View(new UserFormViewModel { AvailableMentors = LoadMentorOptions() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Autor: Tarik Polat
    /// <summary>
    /// Speichert einen neuen Benutzer aus dem Formular.
    /// </summary>
    /// <param name="vm">Die eingegebenen Formulardaten.</param>
    public async Task<IActionResult> Create(UserFormViewModel vm)
    {
        vm.AvailableMentors = LoadMentorOptions();

        ValidateRoleFields(vm);
        ValidatePassword(vm, isNew: true);

        if (_userRepository.EmailExists(vm.Email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Diese E-Mail-Adresse wird bereits verwendet.");
        }

        if (!ModelState.IsValid)
        {
            SetHeader("Benutzer anlegen");
            return View(vm);
        }

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            Email = vm.Email,
            Role = vm.Role,
            PasswordHash = PasswordHasher.Hash(vm.Password!),
            TraineeStartDate = vm.Role == AppConstants.Roles.Trainee ? vm.TraineeStartDate : null,
            TraineeEndDate = vm.Role == AppConstants.Roles.Trainee ? vm.TraineeEndDate : null,
        };

        _userRepository.Add(user);

        if (user.Role == AppConstants.Roles.Trainee)
        {
            _mentorAssignmentRepository.SetMentorsForTrainee(user.Id, vm.AssignedMentorIds);
            await _progressRepository.CreateInitialProgressAsync(user.Id);
        }

        return RedirectToAction(nameof(Index));
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Zeigt das Formular zum Bearbeiten eines Benutzers.
    /// </summary>
    /// <param name="id">Id des Benutzers.</param>
    public IActionResult Edit(string id)
    {
        SetHeader("Benutzer bearbeiten");

        var user = _userRepository.GetById(id);
        if (user == null) return NotFound();

        var vm = new UserFormViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            TraineeStartDate = user.TraineeStartDate,
            TraineeEndDate = user.TraineeEndDate,
            AvailableMentors = LoadMentorOptions(),
            AssignedMentorIds = user.Role == AppConstants.Roles.Trainee
                ? _mentorAssignmentRepository.GetMentorIdsForTrainee(id)
                : new List<string>(),
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Autor: Tarik Polat
    /// <summary>
    /// Speichert die Änderungen an einem Benutzer. Die Rolle kann dabei nicht geändert werden.
    /// </summary>
    /// <param name="id">Id des Benutzers.</param>
    /// <param name="vm">Die geänderten Formulardaten.</param>
    public IActionResult Edit(string id, UserFormViewModel vm)
    {
        var user = _userRepository.GetById(id);
        if (user == null) return NotFound();

        vm.Id = id;
        vm.Role = user.Role;
        vm.AvailableMentors = LoadMentorOptions();

        ValidateRoleFields(vm);
        ValidatePassword(vm, isNew: false);

        if (_userRepository.EmailExists(vm.Email, excludingId: id))
        {
            ModelState.AddModelError(nameof(vm.Email), "Diese E-Mail-Adresse wird bereits verwendet.");
        }

        if (!ModelState.IsValid)
        {
            SetHeader("Benutzer bearbeiten");
            return View(vm);
        }

        user.FirstName = vm.FirstName;
        user.LastName = vm.LastName;
        user.Email = vm.Email;

        if (!string.IsNullOrWhiteSpace(vm.Password))
        {
            user.PasswordHash = PasswordHasher.Hash(vm.Password);
        }

        if (user.Role == AppConstants.Roles.Trainee)
        {
            user.TraineeStartDate = vm.TraineeStartDate;
            user.TraineeEndDate = vm.TraineeEndDate;
        }

        _userRepository.Update(user);

        if (user.Role == AppConstants.Roles.Trainee)
        {
            _mentorAssignmentRepository.SetMentorsForTrainee(user.Id, vm.AssignedMentorIds);
        }

        return RedirectToAction(nameof(Index));
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Zeigt die Seite zum Sperren oder Wiedereröffnen eines Kontos.
    /// </summary>
    /// <param name="id">Id des Benutzers.</param>
    public IActionResult Close(string id)
    {
        ViewData["Eyebrow"] = $"{User.GetRole()} | {User.GetFullName()}";

        var user = _userRepository.GetById(id);
        if (user == null) return NotFound();

        var vm = new CloseAccountViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsClosed = user.IsClosed,
        };

        ViewData["Title"] = vm.IsClosed ? "Konto reaktivieren" : "Konto deaktivieren";
        return View(vm);
    }

    [HttpPost, ActionName("Close")]
    [ValidateAntiForgeryToken]
    // Autor: Tarik Polat
    /// <summary>
    /// Sperrt oder öffnet ein Konto nach der Bestätigung.
    /// </summary>
    /// <param name="id">Id des Benutzers.</param>
    public IActionResult CloseConfirmed(string id)
    {
        var user = _userRepository.GetById(id);
        if (user == null) return NotFound();

        user.IsClosed = !user.IsClosed;
        _userRepository.Update(user);

        return RedirectToAction(nameof(Index));
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Setzt Titel und Eyebrow-Text für die Seite.
    /// </summary>
    /// <param name="title">Der anzuzeigende Titel.</param>
    private void SetHeader(string title)
    {
        ViewData["Title"] = title;
        ViewData["Eyebrow"] = $"{User.GetRole()} | {User.GetFullName()}";
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Prüft die Trainee-Felder wie Start- und Enddatum.
    /// </summary>
    /// <param name="vm">Die zu prüfenden Formulardaten.</param>
    private void ValidateRoleFields(UserFormViewModel vm)
    {
        if (vm.Role is not (AppConstants.Roles.Trainee or AppConstants.Roles.Mentor or AppConstants.Roles.Admin))
            ModelState.AddModelError(nameof(vm.Role), "Ungültige Benutzerrolle.");
        var mentorIds = LoadMentorOptions().Select(mentor => mentor.Id).ToHashSet();
        if (vm.AssignedMentorIds.Any(id => !mentorIds.Contains(id)))
            ModelState.AddModelError(nameof(vm.AssignedMentorIds), "Ungültige Mentor-Zuordnung.");

        if (vm.Role != AppConstants.Roles.Trainee) return;

        if (vm.TraineeStartDate == null)
            ModelState.AddModelError(nameof(vm.TraineeStartDate), "Das Startdatum ist für Trainees erforderlich.");

        if (vm.TraineeEndDate == null)
            ModelState.AddModelError(nameof(vm.TraineeEndDate), "Das Enddatum ist für Trainees erforderlich.");

        if (vm.TraineeStartDate.HasValue && vm.TraineeEndDate.HasValue && vm.TraineeStartDate >= vm.TraineeEndDate)
            ModelState.AddModelError(nameof(vm.TraineeEndDate), "Das Enddatum muss nach dem Startdatum liegen.");
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Prüft das Passwort auf Pflichtangabe und Mindestlänge.
    /// </summary>
    /// <param name="vm">Die zu prüfenden Formulardaten.</param>
    /// <param name="isNew">Gibt an, ob ein neuer Benutzer angelegt wird.</param>
    private void ValidatePassword(UserFormViewModel vm, bool isNew)
    {
        if (isNew && string.IsNullOrWhiteSpace(vm.Password))
        {
            ModelState.AddModelError(nameof(vm.Password), "Das Passwort ist erforderlich.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(vm.Password) && vm.Password.Length < 7)
        {
            ModelState.AddModelError(nameof(vm.Password), "Das Passwort muss mindestens 7 Zeichen enthalten.");
        }
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Lädt die Liste der auswählbaren Mentoren.
    /// </summary>
    private List<MentorOptionViewModel> LoadMentorOptions() =>
        _userRepository.GetMentors()
            .Select(m => new MentorOptionViewModel { Id = m.Id, FullName = m.FullName })
            .ToList();

    // Autor: Tarik Polat
    /// <summary>
    /// Wandelt einen Benutzer in ein Anzeige-ViewModel für die Liste um.
    /// </summary>
    /// <param name="user">Der Benutzer.</param>
    /// <param name="mentorNamesByTraineeId">Zuordnung von Trainee-Id zu Mentor-Namen.</param>
    private static UserListItemViewModel ToListItem(User user, Dictionary<string, List<string>> mentorNamesByTraineeId) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        IsClosed = user.IsClosed,
        TraineeStartDate = user.TraineeStartDate,
        TraineeEndDate = user.TraineeEndDate,
        AssignedMentorNames = mentorNamesByTraineeId.TryGetValue(user.Id, out var names) ? names : new List<string>(),
    };
}
