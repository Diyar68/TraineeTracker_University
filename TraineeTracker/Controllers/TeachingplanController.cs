using Microsoft.AspNetCore.Mvc;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using TraineeTracker.Services;

// Joel Bitterlich für die Individuelle Speicherung der Lektionen Rheifenfolge

namespace TraineeTracker.Controllers;

[TraineeTracker.Filters.RequireDemoRole(AppConstants.RoleValues.Trainee, AppConstants.RoleValues.Mentor, AppConstants.RoleValues.Admin)]
[Route("[controller]/[action]")]
public class TeachingplanController : Controller
{
    private readonly ILectionProgressRepository _progressRepository;
    private readonly ICurrentUserService _currentUser;

    public TeachingplanController(
        ILectionProgressRepository progressRepository,
        ICurrentUserService currentUser)
    {
        _progressRepository = progressRepository;
        _currentUser = currentUser;
    }

    // Joel Bitterlich Sortierung der Lektionen 
    // pro Trainee mithilfe von IndividualOrderIndex
    public class OrderItemDto
    {
        public int lessonId { get; set; }
        public int? individualOrderIndex { get; set; }
        public int? orderIndex { get; set; }
    }

    // Joel Bitterlich Speichert die individuelle Reihenfolge der Lektionen
    //  pro Trainee mithilfe von IndividualOrderIndex

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOrder([FromBody] List<OrderItemDto> order)
    {
        if (order == null) return BadRequest();

        var traineeId = _currentUser.TraineeId;
        if (string.IsNullOrWhiteSpace(traineeId)) return Unauthorized();

        foreach (var item in order)
        {
            var index = item.individualOrderIndex ?? item.orderIndex;
            if (index is null) continue;

            var progress = await _progressRepository.GetByLectionIdAsync(item.lessonId, traineeId);
            if (progress == null)
            {
                progress = new LectionProgress
                {
                    TraineeId = traineeId,
                    LectionId = item.lessonId,
                    Status = LectionStatus.Open,
                };
            }

            progress.IndividualOrderIndex = index.Value;
            await _progressRepository.SaveAsync(progress);
        }

        return Ok();
    }
}
