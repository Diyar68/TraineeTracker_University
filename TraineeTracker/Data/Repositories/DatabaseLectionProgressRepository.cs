// Data/Repositories/DatabaseLectionProgressRepository.cs
using Microsoft.EntityFrameworkCore;
using TraineeTracker.Models;

namespace TraineeTracker.Data.Repositories;

public class DatabaseLectionProgressRepository : ILectionProgressRepository
{
    private readonly AppDbContext _context;

    public DatabaseLectionProgressRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gibt alle Progress-Einträge eines Trainees zurück.
    /// Wird in Index() verwendet um die gesamte Lektionsliste aufzubauen.
    /// </summary>
    public async Task<List<LectionProgress>> GetByTraineeIdAsync(string traineeId)
    {
        return await _context.LectionProgresses
            .Where(p => p.TraineeId == traineeId)
            .ToListAsync();
    }

    /// <summary>
    /// Gibt den Progress-Eintrag für eine bestimmte Lektion zurück.
    /// Filtert immer zusätzlich auf DevTraineeId — kein Trainee
    /// darf den Fortschritt eines anderen lesen oder überschreiben.
    /// </summary>
    public async Task<LectionProgress?> GetByLectionIdAsync(int lectionId, string traineeId)
    {
        return await _context.LectionProgresses
            .FirstOrDefaultAsync(p =>
                p.LectionId == lectionId &&
                p.TraineeId == traineeId);
    }

    /// <summary>
    /// Speichert einen bestehenden Progress-Eintrag.
    /// Legt keinen neuen Eintrag an — das übernimmt der DbInitializer
    /// bzw. später eine separate CreateAsync-Methode.
    /// </summary>
    public async Task SaveAsync(LectionProgress progress)
    {
        _context.LectionProgresses.Update(progress);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Wird beim Anlegen eines neuen Trainees aufgerufen, damit der Lehrplan im Dashboard
    /// sofort sichtbar ist. Ohne diese Einträge zeigt der Lehrplan-Tab keine Lektionen an,
    /// bis der Trainee zufällig selbst einen Fortschrittseintrag erzeugt.
    /// </summary>
    public async Task CreateInitialProgressAsync(string traineeId)
    {
        var activeLectionIds = await _context.Lections
            .Where(l => l.IsActive)
            .Select(l => l.Id)
            .ToListAsync();

        var existingLectionIds = await _context.LectionProgresses
            .Where(p => p.TraineeId == traineeId)
            .Select(p => p.LectionId)
            .ToListAsync();

        var missingLectionIds = activeLectionIds.Except(existingLectionIds);

        foreach (var lectionId in missingLectionIds)
        {
            _context.LectionProgresses.Add(new LectionProgress
            {
                TraineeId = traineeId,
                LectionId = lectionId,
                Status = LectionStatus.Open,
            });
        }

        await _context.SaveChangesAsync();
    }
}
