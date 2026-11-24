using TraineeTracker.Models;

namespace TraineeTracker.Data.Repositories;

/// <summary>
/// Kapselt den Datenbankzugriff für die Zuordnung von Mentoren zu Trainees.
/// </summary>
public class MentorAssignmentRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Erstellt das Repository mit dem Datenbankkontext.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    public MentorAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gibt die Ids der Mentoren eines Trainees zurück.
    /// </summary>
    /// <param name="traineeId">Id des Trainees.</param>
    public List<string> GetMentorIdsForTrainee(string traineeId) =>
        _context.TraineeMentorAssignments
            .Where(a => a.TraineeId == traineeId)
            .Select(a => a.MentorId)
            .ToList();

    /// <summary>
    /// Gibt die Ids der Trainees eines Mentors zurück.
    /// </summary>
    /// <param name="mentorId">Id des Mentors.</param>
    public List<string> GetTraineeIdsForMentor(string mentorId) =>
        _context.TraineeMentorAssignments
            .Where(a => a.MentorId == mentorId)
            .Select(a => a.TraineeId)
            .ToList();

    /// <summary>
    /// Prüft, ob ein Trainee einem Mentor zugewiesen ist.
    /// </summary>
    /// <param name="traineeId">Id des Trainees.</param>
    /// <param name="mentorId">Id des Mentors.</param>
    public bool IsTraineeAssignedToMentor(string? traineeId, string? mentorId)
    {
        if (string.IsNullOrEmpty(traineeId) || string.IsNullOrEmpty(mentorId)) return false;
        return _context.TraineeMentorAssignments.Any(a => a.TraineeId == traineeId && a.MentorId == mentorId);
    }

    /// <summary>
    /// Liefert für jeden Trainee die Namen seiner zugewiesenen Mentoren.
    /// </summary>
    public Dictionary<string, List<string>> GetMentorNamesByTraineeId()
    {
        return _context.TraineeMentorAssignments
            .Join(_context.Users, a => a.MentorId, u => u.Id, (a, u) => new { a.TraineeId, u.FirstName, u.LastName, u.IsClosed })
            .GroupBy(x => x.TraineeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.IsClosed ? $"{x.FirstName} {x.LastName} (geschlossen)" : $"{x.FirstName} {x.LastName}").ToList());
    }

    /// <summary>
    /// Setzt die Mentoren eines Trainees neu.
    /// </summary>
    /// <param name="traineeId">Id des Trainees.</param>
    /// <param name="mentorIds">Ids der zuzuweisenden Mentoren.</param>
    public void SetMentorsForTrainee(string traineeId, List<string> mentorIds)
    {
        var existing = _context.TraineeMentorAssignments.Where(a => a.TraineeId == traineeId);
        _context.TraineeMentorAssignments.RemoveRange(existing);

        foreach (var mentorId in mentorIds.Distinct())
        {
            _context.TraineeMentorAssignments.Add(new TraineeMentorAssignment { TraineeId = traineeId, MentorId = mentorId });
        }

        _context.SaveChanges();
    }
}
