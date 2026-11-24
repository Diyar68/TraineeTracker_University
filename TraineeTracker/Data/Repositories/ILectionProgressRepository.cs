// Data/Repositories/ILectionProgressRepository.cs
using TraineeTracker.Models;

namespace TraineeTracker.Data.Repositories;

public interface ILectionProgressRepository
{
    Task<List<LectionProgress>> GetByTraineeIdAsync(string traineeId);
    Task<LectionProgress?> GetByLectionIdAsync(int lectionId, string traineeId);
    Task SaveAsync(LectionProgress progress);

    /// <summary>Legt für einen (neuen) Trainee "Open"-Fortschrittseinträge für alle aktiven Lektionen an, die noch keinen Eintrag haben.</summary>
    Task CreateInitialProgressAsync(string traineeId);
}