// Diyar
using TraineeTracker.Helpers;
using TraineeTracker.Models;

namespace TraineeTracker.ViewModels;

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class TransitionResultViewModel
{
    public string PreviousStatus { get; init; } = "";
    public string Status { get; init; } = "";
    public string StatusLabel { get; init; } = "";
    public string Title { get; init; } = "";
    public string CardHtml { get; init; } = "";
    public string ModalContentHtml { get; init; } = "";
    public TransitionStatsViewModel Stats { get; init; } = new();

    public static TransitionResultViewModel From(
        Lection lection,
        LectionProgress progress,
        LectionStatus previousStatus,
        DashboardStats stats,
        string cardHtml = "",
        string modalContentHtml = "")
    {
        return new TransitionResultViewModel
        {
            PreviousStatus = previousStatus.ToString().ToLower(),
            Status = progress.Status.ToString().ToLower(),
            StatusLabel = progress.Status.DisplayName(),
            Title = lection.Title,
            CardHtml = cardHtml,
            ModalContentHtml = modalContentHtml,
            Stats = TransitionStatsViewModel.From(stats),
        };
    }
}

public class TransitionStatsViewModel
{
    public int Total { get; init; }
    public int Rated { get; init; }
    public int Pct { get; init; }
    public string AttendedPt { get; init; } = "";
    public string DonePt { get; init; } = "";
    public string OpenPt { get; init; } = "";
    public string BufferPt { get; init; } = "";
    public bool BufferNeg { get; init; }
    public string Velocity { get; init; } = "";
    public string ForecastBufferPt { get; init; } = "";
    public bool ForecastBufferNeg { get; init; }
    public Dictionary<string, int> CountByStatus { get; init; } = new();
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
    public string HeaderSummaryText { get; init; } = "";
    public int AttentionCount { get; init; }
    public int ReviewCount { get; init; }
    public string HeaderBufferText { get; init; } = "";
    public string HeaderBufferClass { get; init; } = "";
    public string NavBufferText { get; init; } = "";
    public string NavBufferClass { get; init; } = "";

    public static TransitionStatsViewModel From(DashboardStats stats)
    {
        var display = StatisticsDisplayViewModel.From(stats);

        return new TransitionStatsViewModel
        {
            Total = stats.TotalCount,
            Rated = stats.RatedCount,
            Pct = stats.ProgressPercent,
            AttendedPt = stats.AttendedPT?.ToString("0.0") ?? "",
            DonePt = stats.DonePT.ToString("0.0"),
            OpenPt = stats.OpenPT.ToString("0.0"),
            BufferPt = stats.BufferPT?.ToString("0.0") ?? "",
            BufferNeg = stats.BufferPT < 0,
            Velocity = stats.VelocityPercent?.ToString("0.#") ?? "",
            ForecastBufferPt = stats.ForecastBufferPT?.ToString("0.0") ?? "",
            ForecastBufferNeg = stats.ForecastBufferPT < 0,
            CountByStatus = Enum.GetValues<LectionStatus>()
                .ToDictionary(s => s.ToString().ToLower(), s => stats.CountByStatus.GetValueOrDefault(s, 0)),
            AttendedText = display.AttendedText,
            DoneText = display.DoneText,
            OpenText = display.OpenText,
            BufferText = display.BufferText,
            BufferToneClass = display.BufferToneClass,
            VelocityText = display.VelocityText,
            VelocityToneClass = display.VelocityToneClass,
            ForecastText = display.ForecastText,
            ForecastToneClass = display.ForecastToneClass,
            AttendedWidthCss = display.AttendedWidthCss,
            DoneWidthCss = display.DoneWidthCss,
            DoneShareCss = display.DoneShareCss,
            OpenShareCss = display.OpenShareCss,
            DoneShareText = display.DoneShareText,
            OpenShareText = display.OpenShareText,
            HeaderSummaryText = stats.HeaderSummaryText,
            AttentionCount = stats.AttentionCount,
            ReviewCount = stats.ReviewCount,
            HeaderBufferText = stats.HeaderBufferText,
            HeaderBufferClass = stats.HeaderBufferClass,
            NavBufferText = stats.BufferPT is decimal buffer
                ? $"{(buffer < 0 ? "−" : "+")}{Math.Abs(buffer):0.0} PT"
                : "–",
            NavBufferClass = stats.BufferPT < 0 ? "puffer-val neg" : "puffer-val",
        };
    }
}
