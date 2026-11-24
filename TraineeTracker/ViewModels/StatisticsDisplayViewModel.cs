using System.Globalization;
using TraineeTracker.Helpers;
using TraineeTracker.Models;

namespace TraineeTracker.ViewModels;

public class StatisticsDisplayViewModel
{
    private static readonly CultureInfo German = new("de-DE");
    public string AttendedText { get; init; } = "";
    public string DoneText { get; init; } = "";
    public string OpenText { get; init; } = "";
    public string BufferText { get; init; } = "";
    public string BufferToneClass { get; init; } = "";
    public string VelocityText { get; init; } = "";
    public string VelocityToneClass { get; init; } = "";
    public string ForecastText { get; init; } = "";
    public string ForecastToneClass { get; init; } = "";
    public string AttendedWidthCss { get; init; } = "";
    public string DoneWidthCss { get; init; } = "";
    public string DoneShareCss { get; init; } = "";
    public string OpenShareCss { get; init; } = "";
    public string DoneShareText { get; init; } = "";
    public string OpenShareText { get; init; } = "";
    public bool IsApiAvailable { get; init; }
    public List<StatisticsStatusRowViewModel> StatusRows { get; init; } = new();

    public static StatisticsDisplayViewModel From(DashboardStats stats)
    {
        var totalPlanPt = stats.DonePT + stats.OpenPT;
        var attendedPT = stats.AttendedPT ?? 0m;
        var comparisonMaxPt = Math.Max(attendedPT, stats.DonePT);
        var attendedWidth = comparisonMaxPt > 0 ? Math.Min(100, attendedPT / comparisonMaxPt * 100) : 0;
        var doneWidth = comparisonMaxPt > 0 ? Math.Min(100, stats.DonePT / comparisonMaxPt * 100) : 0;
        var doneShare = totalPlanPt > 0 ? Math.Min(100, stats.DonePT / totalPlanPt * 100) : 0;
        var openShare = totalPlanPt > 0 ? Math.Min(100, stats.OpenPT / totalPlanPt * 100) : 0;

        return new StatisticsDisplayViewModel
        {
            AttendedText = stats.AttendedPT is decimal attended ? $"{attended.ToString("0.0", German)} PT" : "–",
            DoneText = $"{stats.DonePT.ToString("0.0", German)} PT",
            OpenText = $"{stats.OpenPT.ToString("0.0", German)} PT",
            BufferText = stats.BufferPT is decimal buffer ? SignedPt(buffer) : "–",
            BufferToneClass = stats.BufferPT is decimal bufferValue ? Tone(bufferValue >= 0) : "",
            VelocityText = stats.VelocityPercent is decimal velocity ? $"{velocity:0.#}%" : "–",
            VelocityToneClass = stats.VelocityPercent is decimal velocityValue ? Tone(velocityValue >= 100) : "",
            ForecastText = stats.ForecastBufferPT is decimal forecast ? SignedPt(forecast) : "–",
            ForecastToneClass = stats.ForecastBufferPT is decimal forecastValue ? Tone(forecastValue >= 0) : "",
            AttendedWidthCss = CssPercent(attendedWidth),
            DoneWidthCss = CssPercent(doneWidth),
            DoneShareCss = CssPercent(doneShare),
            OpenShareCss = CssPercent(openShare),
            DoneShareText = $"{doneShare:0}%",
            OpenShareText = $"{openShare:0}%",
            IsApiAvailable = stats.IsApiAvailable,
            StatusRows = Enum.GetValues<LectionStatus>()
                .Select(status => new StatisticsStatusRowViewModel
                {
                    Label = status.DisplayName(),
                    CssKey = status.ToString().ToLower(),
                    Count = stats.CountByStatus.GetValueOrDefault(status, 0),
                })
                .ToList(),
        };
    }

    private static string SignedPt(decimal value) =>
        $"{(value < 0 ? "" : "+")}{value.ToString("0.0", German)} PT";

    private static string Tone(bool positive) => positive ? "positive" : "negative";

    private static string CssPercent(decimal value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}

public class StatisticsStatusRowViewModel
{
    public string Label { get; init; } = "";
    public string CssKey { get; init; } = "";
    public int Count { get; init; }
}
