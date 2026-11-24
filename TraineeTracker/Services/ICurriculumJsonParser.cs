namespace TraineeTracker.Services;

public interface ICurriculumJsonParser
{
    Task<IReadOnlyList<ImportedLection>> ParseAsync(Stream jsonStream, CancellationToken cancellationToken = default);
}
