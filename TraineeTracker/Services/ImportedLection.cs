namespace TraineeTracker.Services;

public sealed record ImportedLection(
    long ExternalId,
    string Number,
    string Title,
    decimal Effort,
    string ExternalLink,
    int OrderIndex,
    bool IsDeprecated);
