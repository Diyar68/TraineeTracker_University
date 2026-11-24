// Diyar
using TraineeTracker.Models;
using TraineeTracker.ViewModels;

namespace TraineeTracker.Services;

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class DashboardGroupingService
{
    private static readonly Dictionary<LectionStatus, int> StatusPriority = new()
    {
        { LectionStatus.Rejected, 0 },
        { LectionStatus.Started,  1 },
        { LectionStatus.Open,     2 },
        { LectionStatus.Accepted, 3 },
        { LectionStatus.Finished, 4 },
        { LectionStatus.Rated,    5 },
        { LectionStatus.Skipped,  6 },
    };

    private static readonly List<LectionGroup> GroupDefinitions = new()
    {
        new("Handlungsbedarf", LectionStatus.Rejected, LectionStatus.Started),
        new("Als Nächstes", LectionStatus.Open),
        new("Feedback offen", LectionStatus.Accepted),
        new("Wartet auf Review", LectionStatus.Finished),
        new("Abgeschlossen", LectionStatus.Rated),
        new("Übersprungen", LectionStatus.Skipped),
    };

    public List<LectionGroup> BuildGroupedLections(List<LectionWithProgress> items)
    {
        var groups = GroupDefinitions.Select(def => new LectionGroup(def.Label, def.Statuses)
        {
            Items = SortGroupItems(def, items
                .Where(x => def.Statuses.Contains(x.Status))
                .ToList())
                .Select(item => new LectionCardViewModel(item, this))
                .ToList(),
        }).ToList();

        return groups.Where(g => g.Items.Any()).ToList();
    }

    public int GetStatusPriority(LectionStatus status)
    {
        return StatusPriority.GetValueOrDefault(status, 99);
    }

    public string GetGroupLabel(LectionStatus status)
    {
        return GroupDefinitions
            .FirstOrDefault(group => group.Statuses.Contains(status))
            ?.Label ?? "Weitere";
    }

    public int GetGroupOrder(LectionStatus status)
    {
        var index = GroupDefinitions.FindIndex(group => group.Statuses.Contains(status));
        return index >= 0 ? index : 99;
    }

    public bool UsesStatusPriority(LectionStatus status)
    {
        return GetGroupLabel(status) == "Handlungsbedarf";
    }

    private static List<LectionWithProgress> SortGroupItems(LectionGroup group, List<LectionWithProgress> items)
    {
        return group.Label == "Handlungsbedarf"
            ? items
                .OrderBy(x => StatusPriority.GetValueOrDefault(x.Status, 99))
                .ThenBy(x => TraineeOrder(x))
                .ThenBy(x => x.Lection.Number)
                .ToList()
            : items
                .OrderBy(x => TraineeOrder(x))
                .ThenBy(x => x.Lection.Number)
                .ToList();
    }

    private static int TraineeOrder(LectionWithProgress item)
    {
        if (item.Progress?.IndividualOrderIndex is int individualOrderIndex)
        {
            return individualOrderIndex < 0 ? int.MaxValue : individualOrderIndex;
        }

        return item.Lection.OrderIndex < 0 ? int.MaxValue : item.Lection.OrderIndex;
    }
}
