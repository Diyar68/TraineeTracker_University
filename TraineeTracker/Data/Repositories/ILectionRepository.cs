// Data/Repositories/ILectionRepository.cs
using TraineeTracker.Models;

namespace TraineeTracker.Data.Repositories;

public interface ILectionRepository
{
    Task<List<Lection>> GetAllAsync();
    Task<Lection?> GetByIdAsync(int id);
}
