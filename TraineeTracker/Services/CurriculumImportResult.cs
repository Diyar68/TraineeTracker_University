namespace TraineeTracker.Services;

public sealed record CurriculumImportResult(
    int Created,
    int Updated,
    int Deprecated,
    int Unchanged,
    IReadOnlyList<string> Warnings)
;
