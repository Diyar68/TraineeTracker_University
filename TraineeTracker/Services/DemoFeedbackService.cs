using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Helpers;
using TraineeTracker.Models;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Services;

/// <summary>
/// Baut die Feedback-Ansichten für Trainee, Mentor und Admin auf.
/// </summary>
public class DemoFeedbackService
{
    private readonly ILectionRepository _lectionRepo;
    private readonly ILectionProgressRepository _progressRepo;
    private readonly FeedbackRepository _feedbackRepo;
    private readonly MentorAssignmentRepository _mentorAssignmentRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Erstellt den Service mit den benötigten Repositories.
    /// </summary>
    /// <param name="lectionRepo">Repository für Lektionen.</param>
    /// <param name="progressRepo">Repository für den Lernfortschritt.</param>
    /// <param name="feedbackRepo">Repository für Feedback.</param>
    /// <param name="mentorAssignmentRepo">Repository für Mentor-Zuordnungen.</param>
    /// <param name="currentUser">Aktuell angemeldeter Benutzer.</param>
    /// <param name="httpContextAccessor">Zugriff auf den aktuellen HTTP-Kontext.</param>
    public DemoFeedbackService(
        ILectionRepository lectionRepo,
        ILectionProgressRepository progressRepo,
        FeedbackRepository feedbackRepo,
        MentorAssignmentRepository mentorAssignmentRepo,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _lectionRepo = lectionRepo;
        _progressRepo = progressRepo;
        _feedbackRepo = feedbackRepo;
        _mentorAssignmentRepo = mentorAssignmentRepo;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>Der aktuell angemeldete Benutzer.</summary>
    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <summary>Id des angemeldeten Benutzers.</summary>
    private string? AuthenticatedUserId => Principal?.GetUserId();

    /// <summary>Gibt an, ob der angemeldete Benutzer Admin ist.</summary>
    private bool IsAdmin => string.Equals(Principal?.GetRole(), AppConstants.Roles.Admin, StringComparison.OrdinalIgnoreCase);

    /// <summary>Gibt an, ob der angemeldete Benutzer Mentor ist.</summary>
    private bool IsMentor => string.Equals(Principal?.GetRole(), AppConstants.Roles.Mentor, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Baut alle Feedback-Daten für das Dashboard zusammen.
    /// </summary>
    public async Task<TraineeDashboardFeedbackViewModel> BuildDashboardFeedbackAsync()
    {
        var lections = await _lectionRepo.GetAllAsync();
        var progress = await _progressRepo.GetByTraineeIdAsync(_currentUser.TraineeId);

        var acceptedLessons = lections
            .Select(l => new LectionWithProgress(l, progress.FirstOrDefault(p => p.LectionId == l.Id)))
            .Where(i => i.Status == LectionStatus.Accepted)
            .ToList();

        return new TraineeDashboardFeedbackViewModel
        {
            OwnFeedback = GetOwnFeedback(),
            OthersFeedback = GetOthersFeedback(),
            MentorFeedback = GetMentorFeedback(),
            AcceptedLessons = acceptedLessons,
        };
    }

    /// <summary>
    /// Gibt das eigene Feedback des Trainees zurück.
    /// </summary>
    public List<FeedbackEntryViewModel> GetOwnFeedback() =>
        _feedbackRepo.GetForTrainee(_currentUser.TraineeId).Select(x => ToViewModel(x, isOwn: true)).ToList();

    /// <summary>
    /// Gibt das Feedback aller anderen Trainees zurück.
    /// </summary>
    public List<FeedbackEntryViewModel> GetOthersFeedback() =>
        _feedbackRepo.GetExcludingTrainee(_currentUser.TraineeId).Select(x => ToViewModel(x, isOwn: false)).ToList();

    /// <summary>
    /// Gibt das Feedback der zugewiesenen Trainees zurück, Admins sehen alles.
    /// </summary>
    public List<FeedbackEntryViewModel> GetMentorFeedback()
    {
        if (IsAdmin)
            return _feedbackRepo.GetAll().Select(x => ToViewModel(x, isOwn: false)).ToList();

        if (!IsMentor || AuthenticatedUserId == null)
            return new List<FeedbackEntryViewModel>();

        var traineeIds = _mentorAssignmentRepo.GetTraineeIdsForMentor(AuthenticatedUserId);
        return _feedbackRepo.GetForTrainees(traineeIds).Select(x => ToViewModel(x, isOwn: false)).ToList();
    }

    /// <summary>
    /// Legt Feedback für die eigene Lektion anhand der Lektions-Id an.
    /// </summary>
    /// <param name="lectionId">Id der Lektion.</param>
    /// <param name="difficulty">Schwierigkeit der Lektion.</param>
    /// <param name="priorKnowledge">Vorkenntnisse des Trainees.</param>
    /// <param name="effort">Tatsächlicher Aufwand in Personentagen.</param>
    /// <param name="comment">Anmerkung zum Feedback.</param>
    public async Task<FeedbackEntryViewModel?> CreateOwnFeedbackForLectionAsync(int lectionId, string? difficulty, string? priorKnowledge, decimal? effort, string? comment)
    {
        var progress = await _progressRepo.GetByLectionIdAsync(lectionId, _currentUser.TraineeId);
        if (progress == null) return null;

        var result = _feedbackRepo.CreateForAcceptedProgress(progress.Id, _currentUser.TraineeId, difficulty, priorKnowledge, effort, comment);
        return result == null ? null : ToViewModel(result, isOwn: true);
    }

    /// <summary>
    /// Sucht eigenes Feedback anhand der Feedback-Id.
    /// </summary>
    /// <param name="feedbackId">Id des Feedbacks.</param>
    public FeedbackEntryViewModel? FindOwnFeedbackOrDefault(int feedbackId)
    {
        var match = _feedbackRepo.GetById(feedbackId);
        if (match == null || match.Progress.TraineeId != _currentUser.TraineeId) return null;
        return ToViewModel(match, isOwn: true);
    }

    /// <summary>
    /// Ändert ein eigenes Feedback.
    /// </summary>
    /// <param name="feedbackId">Id des Feedbacks.</param>
    /// <param name="difficulty">Schwierigkeit der Lektion.</param>
    /// <param name="priorKnowledge">Vorkenntnisse des Trainees.</param>
    /// <param name="effort">Tatsächlicher Aufwand in Personentagen.</param>
    /// <param name="comment">Anmerkung zum Feedback.</param>
    public bool UpdateOwnFeedback(int feedbackId, string? difficulty, string? priorKnowledge, decimal? effort, string? comment)
    {
        var existing = _feedbackRepo.GetById(feedbackId);
        if (existing == null || existing.Progress.TraineeId != _currentUser.TraineeId) return false;
        return _feedbackRepo.UpdateFromForm(feedbackId, difficulty, priorKnowledge, effort, comment);
    }

    /// <summary>
    /// Löscht ein eigenes Feedback.
    /// </summary>
    /// <param name="feedbackId">Id des Feedbacks.</param>
    public FeedbackDeletionResult? DeleteOwnFeedback(int feedbackId)
    {
        var existing = _feedbackRepo.GetById(feedbackId);
        if (existing == null || existing.Progress.TraineeId != _currentUser.TraineeId) return null;
        return BuildDeletionResult(existing);
    }

    /// <summary>
    /// Sucht Feedback für die Mentor-Ansicht, wenn der Trainee zugewiesen ist.
    /// </summary>
    /// <param name="feedbackId">Id des Feedbacks.</param>
    public FeedbackEntryViewModel? FindMentorFeedbackOrDefault(int feedbackId)
    {
        var match = _feedbackRepo.GetById(feedbackId);
        if (match == null) return null;
        if (!IsAdmin && !_mentorAssignmentRepo.IsTraineeAssignedToMentor(match.Progress.TraineeId, AuthenticatedUserId)) return null;
        return ToViewModel(match, isOwn: false);
    }

    /// <summary>
    /// Ändert Feedback aus Mentor-Sicht.
    /// </summary>
    /// <param name="feedbackId">Id des Feedbacks.</param>
    /// <param name="difficulty">Schwierigkeit der Lektion.</param>
    /// <param name="priorKnowledge">Vorkenntnisse des Trainees.</param>
    /// <param name="effort">Tatsächlicher Aufwand in Personentagen.</param>
    /// <param name="comment">Anmerkung zum Feedback.</param>
    public bool UpdateMentorFeedback(int feedbackId, string? difficulty, string? priorKnowledge, decimal? effort, string? comment)
    {
        var existing = _feedbackRepo.GetById(feedbackId);
        if (existing == null) return false;
        if (!IsAdmin && !_mentorAssignmentRepo.IsTraineeAssignedToMentor(existing.Progress.TraineeId, AuthenticatedUserId)) return false;
        return _feedbackRepo.UpdateFromForm(feedbackId, difficulty, priorKnowledge, effort, comment);
    }

    /// <summary>
    /// Löscht Feedback aus Mentor-Sicht.
    /// </summary>
    /// <param name="feedbackId">Id des Feedbacks.</param>
    public FeedbackDeletionResult? DeleteMentorFeedback(int feedbackId)
    {
        var existing = _feedbackRepo.GetById(feedbackId);
        if (existing == null) return null;
        if (!IsAdmin && !_mentorAssignmentRepo.IsTraineeAssignedToMentor(existing.Progress.TraineeId, AuthenticatedUserId)) return null;
        return BuildDeletionResult(existing);
    }

    /// <summary>
    /// Löscht das Feedback und baut das Ergebnis für die Anzeige.
    /// </summary>
    /// <param name="existing">Das zu löschende Feedback mit Kontext.</param>
    private FeedbackDeletionResult BuildDeletionResult(FeedbackWithContext existing)
    {
        _feedbackRepo.Delete(existing.Feedback.Id);
        return new FeedbackDeletionResult
        {
            LectionId = existing.Lection.Id,
            LessonTitle = existing.Lection.Title,
        };
    }

    /// <summary>
    /// Wandelt ein Feedback mit Kontext in ein Anzeige-ViewModel um.
    /// </summary>
    /// <param name="x">Das Feedback mit Kontext.</param>
    /// <param name="isOwn">Gibt an, ob es das eigene Feedback ist.</param>
    private static FeedbackEntryViewModel ToViewModel(FeedbackWithContext x, bool isOwn) => new()
    {
        Id = x.Feedback.Id,
        LectionId = x.Lection.Id,
        LessonTitle = x.Lection.Title,
        TraineeName = x.Trainee.FullName,
        SubmittedAt = x.Feedback.CreatedAt,
        Difficulty = FeedbackRepository.ToDifficultyLabel(x.Feedback.DifficultyRating),
        PriorKnowledge = x.Feedback.PriorKnowledge,
        ActualEffortDays = x.Feedback.ActualEffortDays,
        Comment = x.Feedback.Note ?? "",
        IsOwnFeedback = isOwn,
    };
}
