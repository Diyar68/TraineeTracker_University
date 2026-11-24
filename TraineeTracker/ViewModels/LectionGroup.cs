// Diyar
using TraineeTracker.Models;

namespace TraineeTracker.ViewModels
{
    /// <remarks>
    /// Codeownership: Diyar Altintas
    /// </remarks>
    public class LectionGroup
    {
        public string Label { get; }
        public LectionStatus[] Statuses { get; }
        public List<LectionCardViewModel> Items { get; set; } = new();

        public LectionGroup(string label, params LectionStatus[] statuses)
        {
            Label = label;
            Statuses = statuses;
        }
    }
}
