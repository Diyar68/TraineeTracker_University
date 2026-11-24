using System.Reflection;
using System.Globalization;
using TraineeTracker.Helpers;
using TraineeTracker.Models;

namespace TraineeTracker.ViewModels;

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class LessonModalViewModel
{
    public int LectionId { get; init; }
    public string Title { get; init; } = "";
    public string EyebrowText { get; init; } = "";
    public string EffortText { get; init; } = "";
    public string? ExternalLink { get; init; }
    public string Status { get; init; } = "";
    public string StatusLabel { get; init; } = "";
    public List<string> MetaItems { get; init; } = new();
    public List<LessonModalStatusNodeViewModel> StatusChain { get; init; } = new();
    public string? RejectionNote { get; init; }
    public List<LessonModalTransitionViewModel> Transitions { get; init; } = new();

    public static LessonModalViewModel From(
        Lection lection,
        LectionProgress? progress,
        LectionStatus status,
        IEnumerable<object> statusChain,
        IEnumerable<object> transitions)
    {
        return new LessonModalViewModel
        {
            LectionId = lection.Id,
            Title = lection.Title,
            EyebrowText = $"Lektion {lection.Number}",
            EffortText = $"{lection.Effort.ToString("0.#", CultureInfo.InvariantCulture)} PT",
            ExternalLink = CurriculumLinks.ForDemo(lection.ExternalLink),
            Status = status.ToString().ToLowerInvariant(),
            StatusLabel = status.DisplayName(),
            MetaItems = BuildMetaItems(lection, progress),
            StatusChain = statusChain.Select(ToStatusNode).ToList(),
            RejectionNote = progress?.RejectionNote,
            Transitions = transitions.Select(ToTransition).ToList(),
        };
    }

    private static List<string> BuildMetaItems(Lection lection, LectionProgress? progress)
    {
        var items = new List<string> { $"⏱ {lection.Effort.ToString("0.#", CultureInfo.InvariantCulture)} PT" };

        AddDate(items, "▶", "Gestartet am", progress?.StartedAt);
        AddDate(items, "↑", "Eingereicht am", progress?.FinishedAt);
        AddDate(items, "✔", "Akzeptiert am", progress?.AcceptedAt);
        AddDate(items, "✕", "Abgelehnt am", progress?.RejectedAt);
        AddDate(items, "★", "Bewertet am", progress?.RatedAt);

        return items;
    }

    private static void AddDate(List<string> items, string icon, string label, DateTime? value)
    {
        if (value is DateTime date)
            items.Add($"{icon} {label} {date:dd.MM.yyyy}");
    }

    private static LessonModalStatusNodeViewModel ToStatusNode(object value)
    {
        return new LessonModalStatusNodeViewModel
        {
            Status = ReadString(value, "status"),
            Label = ReadString(value, "label"),
            IsCurrent = ReadBool(value, "isCurrent"),
            IsPast = ReadBool(value, "isPast"),
        };
    }

    private static LessonModalTransitionViewModel ToTransition(object value)
    {
        var from = ReadString(value, "from");
        var to = ReadString(value, "to");

        return new LessonModalTransitionViewModel
        {
            From = from,
            To = to,
            Label = ReadString(value, "label"),
            Icon = ReadString(value, "icon"),
            Description = ReadString(value, "desc"),
            IsPrimary = ReadBool(value, "primary"),
            TestId = $"transition-{from}-to-{to}",
        };
    }

    private static string ReadString(object value, string propertyName)
    {
        return ReadProperty(value, propertyName)?.ToString() ?? "";
    }

    private static bool ReadBool(object value, string propertyName)
    {
        return ReadProperty(value, propertyName) is bool result && result;
    }

    private static object? ReadProperty(object value, string propertyName)
    {
        return value.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
            ?.GetValue(value);
    }
}

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class LessonModalStatusNodeViewModel
{
    public string Status { get; init; } = "";
    public string Label { get; init; } = "";
    public bool IsCurrent { get; init; }
    public bool IsPast { get; init; }
}

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class LessonModalTransitionViewModel
{
    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public string Label { get; init; } = "";
    public string Icon { get; init; } = "";
    public string Description { get; init; } = "";
    public bool IsPrimary { get; init; }
    public string TestId { get; init; } = "";
}
