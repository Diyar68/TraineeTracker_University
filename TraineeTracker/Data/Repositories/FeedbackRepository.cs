using TraineeTracker.Models;

namespace TraineeTracker.Data.Repositories;

// Autor: Tarik Polat
/// <summary>
/// Bündelt ein Feedback mit Fortschritt, Lektion und Trainee für die Anzeige.
/// </summary>
public class FeedbackWithContext
{
    /// <summary>Das Feedback selbst.</summary>
    public Feedback Feedback { get; set; } = null!;

    /// <summary>Der zugehörige Lernfortschritt.</summary>
    public LectionProgress Progress { get; set; } = null!;

    /// <summary>Die zugehörige Lektion.</summary>
    public Lection Lection { get; set; } = null!;

    /// <summary>Der Trainee, der das Feedback gegeben hat.</summary>
    public User Trainee { get; set; } = null!;
}

// Autor: Tarik Polat
/// <summary>
/// Bündelt einen abgeschlossenen, aber noch nicht bewerteten Fortschritts-Eintrag mit seiner Lektion.
/// </summary>
public class FinishedProgressInfo
{
    /// <summary>Der Lernfortschritt.</summary>
    public LectionProgress Progress { get; set; } = null!;

    /// <summary>Die zugehörige Lektion.</summary>
    public Lection Lection { get; set; } = null!;
}

// Autor: Tarik Polat
/// <summary>
/// Kapselt den Datenbankzugriff für Feedback-Einträge.
/// </summary>
public class FeedbackRepository
{
    private readonly AppDbContext _context;

    // Autor: Tarik Polat
    /// <summary>
    /// Erstellt das Repository mit dem Datenbankkontext.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    public FeedbackRepository(AppDbContext context)
    {
        _context = context;
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Gibt alle Feedback-Einträge zurück.
    /// </summary>
    public List<FeedbackWithContext> GetAll() => Query().ToList();

    // Autor: Tarik Polat
    /// <summary>
    /// Gibt alle Feedback-Einträge eines Trainees zurück.
    /// </summary>
    /// <param name="traineeId">Id des Trainees.</param>
    public List<FeedbackWithContext> GetForTrainee(string traineeId) =>
        Query().Where(x => x.Progress.TraineeId == traineeId).ToList();

    // Autor: Tarik Polat
    /// <summary>
    /// Gibt alle Feedback-Einträge mehrerer Trainees zurück.
    /// </summary>
    /// <param name="traineeIds">Ids der Trainees.</param>
    public List<FeedbackWithContext> GetForTrainees(IEnumerable<string> traineeIds)
    {
        var ids = traineeIds.ToHashSet();
        return Query().Where(x => ids.Contains(x.Progress.TraineeId)).ToList();
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Gibt alle Feedback-Einträge außer denen eines Trainees zurück.
    /// </summary>
    /// <param name="traineeId">Id des auszuschließenden Trainees.</param>
    public List<FeedbackWithContext> GetExcludingTrainee(string traineeId) =>
        Query().Where(x => x.Progress.TraineeId != traineeId).ToList();

    // Autor: Tarik Polat
    /// <summary>
    /// Legt für eine akzeptierte Lektion ein neues Feedback an und setzt den Fortschritt auf "bewertet".
    /// </summary>
    /// <param name="progressId">Id des Lernfortschritts.</param>
    /// <param name="traineeId">Id des Trainees.</param>
    /// <param name="difficulty">Schwierigkeit der Lektion.</param>
    /// <param name="priorKnowledge">Vorkenntnisse des Trainees.</param>
    /// <param name="effort">Tatsächlicher Aufwand in Personentagen.</param>
    /// <param name="comment">Anmerkung zum Feedback.</param>
    public FeedbackWithContext? CreateForAcceptedProgress(
        int progressId, string traineeId, string? difficulty, string? priorKnowledge, decimal? effort, string? comment)
    {
        var progress = _context.LectionProgresses.FirstOrDefault(p => p.Id == progressId && p.TraineeId == traineeId);
        if (progress == null || progress.Status != LectionStatus.Accepted) return null;

        var feedback = new Feedback
        {
            LectionProgressId = progress.Id,
            DifficultyRating = ToDifficultyRating(difficulty),
            PriorKnowledge = priorKnowledge ?? "",
            ActualEffortDays = effort ?? 0,
            Note = comment,
            CreatedAt = DateTime.Now,
        };

        progress.Status = LectionStatus.Rated;
        progress.RatedAt = DateTime.Now;

        _context.Feedbacks.Add(feedback);
        _context.SaveChanges();

        return GetById(feedback.Id);
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Legt ein leeres Feedback an, falls für den Fortschritt noch keins existiert.
    /// </summary>
    /// <param name="progressId">Id des Lernfortschritts.</param>
    public void CreateEmptyIfMissing(int progressId)
    {
        if (_context.Feedbacks.Any(f => f.LectionProgressId == progressId)) return;

        _context.Feedbacks.Add(new Feedback
        {
            LectionProgressId = progressId,
            DifficultyRating = 0,
            PriorKnowledge = "",
            ActualEffortDays = 0,
            Note = null,
            CreatedAt = DateTime.Now,
        });
        _context.SaveChanges();
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Löscht das Feedback zu einem Lernfortschritt, falls vorhanden.
    /// </summary>
    /// <param name="progressId">Id des Lernfortschritts.</param>
    public void DeleteForProgress(int progressId)
    {
        var feedbacks = _context.Feedbacks.Where(f => f.LectionProgressId == progressId).ToList();
        if (feedbacks.Count == 0) return;

        _context.Feedbacks.RemoveRange(feedbacks);
        _context.SaveChanges();
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Sucht ein Feedback anhand seiner Id.
    /// </summary>
    /// <param name="feedbackId">Id des Feedbacks.</param>
    public FeedbackWithContext? GetById(int feedbackId) => Query().FirstOrDefault(x => x.Feedback.Id == feedbackId);

    // Autor: Tarik Polat
    /// <summary>
    /// Übernimmt die Formulardaten in ein bestehendes Feedback.
    /// </summary>
    /// <param name="feedbackId">Id des Feedbacks.</param>
    /// <param name="difficulty">Schwierigkeit der Lektion.</param>
    /// <param name="priorKnowledge">Vorkenntnisse des Trainees.</param>
    /// <param name="effort">Tatsächlicher Aufwand in Personentagen.</param>
    /// <param name="comment">Anmerkung zum Feedback.</param>
    public bool UpdateFromForm(int feedbackId, string? difficulty, string? priorKnowledge, decimal? effort, string? comment)
    {
        var feedback = _context.Feedbacks.FirstOrDefault(f => f.Id == feedbackId);
        if (feedback == null) return false;

        feedback.DifficultyRating = ToDifficultyRating(difficulty);
        feedback.PriorKnowledge = priorKnowledge ?? "";
        feedback.ActualEffortDays = effort ?? 0;
        feedback.Note = comment;

        _context.Feedbacks.Update(feedback);
        _context.SaveChanges();
        return true;
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Löscht ein Feedback und setzt den zugehörigen Fortschritt bei Bedarf zurück auf "akzeptiert".
    /// </summary>
    /// <param name="feedbackId">Id des Feedbacks.</param>
    public FinishedProgressInfo? Delete(int feedbackId)
    {
        var feedback = _context.Feedbacks.FirstOrDefault(f => f.Id == feedbackId);
        if (feedback == null) return null;

        var progress = _context.LectionProgresses.FirstOrDefault(p => p.Id == feedback.LectionProgressId);
        FinishedProgressInfo? reverted = null;

        if (progress != null && progress.Status == LectionStatus.Rated)
        {
            progress.Status = LectionStatus.Accepted;
            progress.RatedAt = null;
            var lection = _context.Lections.FirstOrDefault(l => l.Id == progress.LectionId);
            if (lection != null)
                reverted = new FinishedProgressInfo { Progress = progress, Lection = lection };
        }

        _context.Feedbacks.Remove(feedback);
        _context.SaveChanges();

        return reverted;
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Wandelt eine Schwierigkeits-Zahl in einen lesbaren Text um.
    /// </summary>
    /// <param name="rating">Die Schwierigkeit als Zahl.</param>
    public static string ToDifficultyLabel(int rating) => rating switch
    {
        1 => "leicht",
        2 => "mittel",
        3 => "schwer",
        _ => "",
    };

    // Autor: Tarik Polat
    /// <summary>
    /// Wandelt einen Schwierigkeits-Text in eine Zahl um.
    /// </summary>
    /// <param name="label">Die Schwierigkeit als Text.</param>
    public static int ToDifficultyRating(string? label) => label switch
    {
        "leicht" => 1,
        "mittel" => 2,
        "schwer" => 3,
        _ => 0,
    };

    // Autor: Tarik Polat
    /// <summary>
    /// Baut die Datenbankabfrage für Feedback mit allen zugehörigen Daten.
    /// </summary>
    private IQueryable<FeedbackWithContext> Query()
    {
        var result =
            from f in _context.Feedbacks
            join p in _context.LectionProgresses on f.LectionProgressId equals p.Id
            join l in _context.Lections on p.LectionId equals l.Id
            join u in _context.Users on p.TraineeId equals u.Id
            orderby f.CreatedAt descending
            select new FeedbackWithContext { Feedback = f, Progress = p, Lection = l, Trainee = u };

        return result;
    }
}
