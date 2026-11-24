// Services/DemoCurrentUserService.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Helpers;
using TraineeTracker.Models;

namespace TraineeTracker.Services;

/// <summary>
/// Löst den "aktuellen Trainee" für die Dashboard-Ansichten auf.
/// Für Trainees ist das immer der eigene Account. Für Mentoren/Admins kann
/// per Cookie ein zugewiesener Trainee ausgewählt werden ("Sicht wechseln") —
/// das Cookie wird dabei serverseitig gegen die echte Mentor-Zuordnung geprüft,
/// damit ein manipuliertes Cookie keine fremden Trainee-Daten öffnen kann.
/// (Name ist historisch, siehe RequireDemoRoleAttribute.)
/// </summary>
public class DemoCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserRepository _userRepository;
    private readonly MentorAssignmentRepository _mentorAssignmentRepository;

    public DemoCurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        UserRepository userRepository,
        MentorAssignmentRepository mentorAssignmentRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
        _mentorAssignmentRepository = mentorAssignmentRepository;
    }

    public string TraineeId => ResolvedTrainee?.Id ?? "";
    public string DisplayName => ResolvedTrainee?.FullName ?? "";
    public string Email => ResolvedTrainee?.Email ?? "";
    public DateTime TraineeStartDate => ResolvedTrainee?.TraineeStartDate ?? DateTime.Today;
    public DateTime TraineeEndDate => ResolvedTrainee?.TraineeEndDate ?? DateTime.Today;

    private User? ResolvedTrainee
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;

            if (user?.Identity?.IsAuthenticated != true) return null;

            var ownId = user.GetUserId();
            if (ownId == null) return null;

            var role = user.FindFirstValue(ClaimTypes.Role);
            var isMentor = string.Equals(role, AppConstants.Roles.Mentor, StringComparison.OrdinalIgnoreCase);
            var isAdmin = string.Equals(role, AppConstants.Roles.Admin, StringComparison.OrdinalIgnoreCase);

            if (!isMentor && !isAdmin)
                return _userRepository.GetById(ownId);

            // Mentor/Admin: nur einen tatsächlich zugewiesenen (bzw. für Admin: irgendeinen aktiven) Trainee erlauben.
            var assignedTraineeIds = isAdmin
                ? _userRepository.GetTrainees().Select(u => u.Id).ToList()
                : _mentorAssignmentRepository.GetTraineeIdsForMentor(ownId);

            var selectedId = httpContext!.Request.Cookies[AppConstants.Cookies.MentorTrainee];
            if (!string.IsNullOrWhiteSpace(selectedId) && assignedTraineeIds.Contains(selectedId))
            {
                var selected = _userRepository.GetById(selectedId);
                if (selected is { Role: AppConstants.Roles.Trainee, IsClosed: false })
                    return selected;
            }

            // Kein gültiges Auswahl-Cookie: Mentor/Admin bleiben in der Übersicht
            // ohne künstlich ausgewählten Trainee-Kontext.
            return null;
        }
    }
}
