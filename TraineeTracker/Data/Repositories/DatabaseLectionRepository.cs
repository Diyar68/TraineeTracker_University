// Data/Repositories/DatabaseLectionRepository.cs
using Microsoft.EntityFrameworkCore;
using TraineeTracker.Models;
using TraineeTracker.Services;

namespace TraineeTracker.Data.Repositories;

public class DatabaseLectionRepository : ILectionRepository, ICurriculumSyncService
{
    private readonly AppDbContext _context;

    public DatabaseLectionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Lection>> GetAllAsync()
    {
        return await _context.Lections
            .OrderBy(l => l.OrderIndex < 0 ? int.MaxValue : l.OrderIndex)
            .ThenBy(l => l.Title)
            .ThenBy(l => l.Id)
            .ToListAsync();
    }

    public async Task<Lection?> GetByIdAsync(int id)
    {
        return await _context.Lections
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<CurriculumImportResult> ImportAsync(
    int curriculumId,
    IReadOnlyList<ImportedLection> importedLections,
    CancellationToken cancellationToken = default)
{
    _ = curriculumId;

    if (importedLections.Count == 0)
    {
        throw new InvalidDataException("Der Lehrplan-Import enthält keine Lektionen.");
    }

    var duplicateExternalId = importedLections
    .GroupBy(l => l.ExternalId)
    .FirstOrDefault(group => group.Count() > 1);

    if (duplicateExternalId is not null)
    {
        throw new InvalidDataException(
            $"Die Importdatei enthält die Lektions-ID '{duplicateExternalId.Key}' mehrfach.");
    }
    var existingLections = await _context.Lections
        .Include(l => l.Progresses)
        .ToListAsync(cancellationToken);

    var existingByExternalId = existingLections.ToDictionary(l => l.ExternalId);
    var importedExternalIds = importedLections.Select(l => l.ExternalId).ToHashSet();
    var warnings = new List<string>();

    var created = 0;
    var updated = 0;
    var unchanged = 0;
    var deprecated = 0;

    foreach (var importedLection in importedLections)
    {
        if (!existingByExternalId.TryGetValue(importedLection.ExternalId, out var lection))
        {
            lection = new Lection
            {
                ExternalId = importedLection.ExternalId,
                Number = importedLection.Number,
                Title = importedLection.Title,
                Effort = importedLection.Effort,
                OrderIndex = importedLection.OrderIndex,
                IsActive = !importedLection.IsDeprecated,
                ExternalLink = importedLection.ExternalLink
            };

            _context.Lections.Add(lection);
            created++;
            continue;
        }

        var wasActive = lection.IsActive;
        var willBeActive = !importedLection.IsDeprecated;
        var hasChanges = ApplyImportedValues(lection, importedLection);

        if (wasActive && !willBeActive)
        {
            deprecated++;

            if (lection.Progresses.Any(p => p.KeepsDeprecatedLectionActive))
            {
                warnings.Add($"Lektion \"{lection.Title}\" wurde importiert deaktiviert, bleibt aber wegen bestehendem Fortschritt sichtbar.");
            }
        }
        else if (hasChanges)
        {
            updated++;
        }
        else
        {
            unchanged++;
        }
    }

    foreach (var lection in existingLections.Where(l => !importedExternalIds.Contains(l.ExternalId)))
    {
        if (!lection.IsActive)
        {
            continue;
        }

        lection.IsActive = false;
        deprecated++;

        if (lection.Progresses.Any(p => p.KeepsDeprecatedLectionActive))
        {
            warnings.Add($"Lektion {lection.Number} fehlt im Import, bleibt aber wegen bestehendem Fortschritt nachvollziehbar.");
        }
    }

    await _context.SaveChangesAsync(cancellationToken);

    return new CurriculumImportResult(created, updated, deprecated, unchanged, warnings);
}

    private static bool ApplyImportedValues(Lection lection, ImportedLection importedLection)
    {
        var isActive = !importedLection.IsDeprecated;
        var hasChanges =
            lection.Number != importedLection.Number ||
            lection.ExternalId != importedLection.ExternalId ||
            lection.Title != importedLection.Title ||
            lection.Effort != importedLection.Effort ||
            lection.OrderIndex != importedLection.OrderIndex ||
            lection.IsActive != isActive ||
            lection.ExternalLink != importedLection.ExternalLink;

        lection.Number = importedLection.Number;
        lection.ExternalId = importedLection.ExternalId;
        lection.Title = importedLection.Title;
        lection.Effort = importedLection.Effort;
        lection.OrderIndex = importedLection.OrderIndex;
        lection.IsActive = isActive;
        lection.ExternalLink = importedLection.ExternalLink;

        return hasChanges;
    }
}
