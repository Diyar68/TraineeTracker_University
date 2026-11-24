namespace TraineeTracker.Services;

public interface ICurriculumSyncService
{
    Task<CurriculumImportResult> ImportAsync(
        int curriculumId,
        IReadOnlyList<ImportedLection> importedLessons,
        CancellationToken cancellationToken = default);
}
