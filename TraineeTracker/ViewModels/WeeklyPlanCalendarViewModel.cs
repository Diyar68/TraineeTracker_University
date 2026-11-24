namespace TraineeTracker.ViewModels;

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class WeeklyPlanViewModel
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string WeekLabel { get; init; } = "";
    public IReadOnlyList<WeeklyPlanDayViewModel> HeaderDays { get; init; } = [];
    public IReadOnlyList<WeeklyPlanWeekViewModel> Weeks { get; init; } = [];
}

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class WeeklyPlanWeekViewModel
{
    public int CalendarWeek { get; init; }
    public IReadOnlyList<WeeklyPlanDayViewModel> Days { get; init; } = [];
}

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class WeeklyPlanDayViewModel
{
    public DateOnly Date { get; init; }
    public string DayName { get; init; } = "";
    public int DayNumber => Date.Day;
    public bool IsToday { get; init; }
    public bool IsPast { get; init; }
    public IReadOnlyList<WeeklyPlanEntryViewModel> Entries { get; init; } = [];

    public string HeaderCssClass =>
        "cal-col-head"
        + (IsToday ? " today-col" : "");

    public string DayCssClass =>
        "cal-day"
        + (IsToday ? " today" : "")
        + (IsPast && !IsToday ? " past" : "");
}

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class WeeklyPlanEntryViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Status { get; init; } = "";
    public string CssClass { get; init; } = "";
    public string Effort { get; init; } = "";
    public string Tooltip { get; init; } = "";
}
