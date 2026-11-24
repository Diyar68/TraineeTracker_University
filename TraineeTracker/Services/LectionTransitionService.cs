// Diyar
using TraineeTracker.Models;
using TraineeTracker.Helpers;

namespace TraineeTracker.Services;

/// <remarks>
/// Codeownership: Diyar Altintas
/// </remarks>
public class LectionTransitionService : ILectionTransitionService
{
    // ═══════════════════════════════════════════════════════
    // TRANSITIONS — DEFINITION
    // ═══════════════════════════════════════════════════════

    private record TransitionDef(
        LectionStatus From,
        LectionStatus To,
        string Label,
        string Icon,
        string Desc,
        bool TraineeAllowed,
        bool Primary = false
    );

    private static readonly List<TransitionDef> AllTransitions = new()
    {
        new(LectionStatus.Open,     LectionStatus.Skipped,  "Überspringen",      "⏭", "Lektion aus Lehrplan entfernen", false),
        new(LectionStatus.Skipped,  LectionStatus.Open,     "Reaktivieren",      "↩", "Lektion wieder aktivieren",      false),
        new(LectionStatus.Open,     LectionStatus.Started,  "Starten",           "▶", "Lektion beginnen",               true,  true),
        new(LectionStatus.Started,  LectionStatus.Open,     "Zurücksetzen",      "↺", "Auf offen setzen",               true),
        new(LectionStatus.Started,  LectionStatus.Finished, "Abschließen",       "✓", "Zur Review einreichen",          true,  true),
        new(LectionStatus.Finished, LectionStatus.Started,  "Zurückziehen",      "↺", "Wieder in Bearbeitung",          true),
        new(LectionStatus.Finished, LectionStatus.Rejected, "Ablehnen",          "✕", "Review nicht bestanden",         false),
        new(LectionStatus.Rejected, LectionStatus.Finished, "Erneut einreichen", "↑", "Nach Korrekturen",               true,  true),
        new(LectionStatus.Finished, LectionStatus.Accepted, "Akzeptieren",       "✔", "Review bestanden",               false, true),
        new(LectionStatus.Accepted, LectionStatus.Rated,    "Bewerten",          "★", "Eigenes Feedback geben",         true,  true),
    };

    public bool IsTransitionAllowed(LectionStatus from, LectionStatus to, bool isMentor)
    {
        if (isMentor) return true; // Ein Mentor darf jeden Status beliebig setzen.

        return (from, to) switch
        {
            (LectionStatus.Open, LectionStatus.Started) => true,
            (LectionStatus.Started, LectionStatus.Open) => true,
            (LectionStatus.Started, LectionStatus.Finished) => true,
            (LectionStatus.Finished, LectionStatus.Started) => true,
            (LectionStatus.Rejected, LectionStatus.Finished) => true,
            (LectionStatus.Accepted, LectionStatus.Rated) => true,
            _ => false,
        };
    }

    public List<object> GetAvailableTransitions(LectionStatus from, bool isMentor)
    {
        // Joel Bitterlich für Mentoren alle Statuswechsel anzeigen
        if (isMentor)
        {
            return Enum.GetValues<LectionStatus>()
                .Where(to => to != from)
                .Select(to => (object)new
                {
                    from = from.ToString().ToLower(),
                    to = to.ToString().ToLower(),
                    label = GetMentorLabel(from, to),
                    icon = GetMentorIcon(to),
                    desc = $"Status auf {to.DisplayName()} setzen",
                    primary = false,
                })
                .ToList();
        }

        var transitions = AllTransitions
            .Where(t => t.From == from && IsVisibleForRole(t, isMentor))
            .Select(t => (object)new
            {
                from = t.From.ToString().ToLower(),
                to = t.To.ToString().ToLower(),
                label = t.Label,
                icon = t.Icon,
                desc = t.Desc,
                primary = t.Primary,
            })
            .ToList();

        return transitions;
    }

    private static bool IsVisibleForRole(TransitionDef transition, bool isMentor)
    {
        if (isMentor)
            return true;
        return transition.TraineeAllowed;
    }

    // Joel Bitterlich für Mentor das richtige Label
    private static string GetMentorLabel(LectionStatus from, LectionStatus to)
    {
        return to switch
        {
            LectionStatus.Open => "Auf Offen setzen",
            LectionStatus.Started => "In Bearbeitung setzen",
            LectionStatus.Finished => "Als Eingereicht markieren",
            LectionStatus.Accepted => "Als Akzeptiert markieren",
            LectionStatus.Rejected => "Als Abgelehnt markieren",
            LectionStatus.Rated => "Als Bewertet markieren",
            LectionStatus.Skipped => "Als Übersprungen markieren",
            _ => $"Status auf {to.DisplayName()} setzen",
        };
    }

// Joel Bitterlich für Mentor das richtige Icon
    private static string GetMentorIcon(LectionStatus to)
    {
        return to switch
        {
            LectionStatus.Open => "↺",
            LectionStatus.Started => "▶",
            LectionStatus.Finished => "✓",
            LectionStatus.Accepted => "✔",
            LectionStatus.Rejected => "✕",
            LectionStatus.Rated => "★",
            LectionStatus.Skipped => "⏭",
            _ => "?",
        };
    }

    public List<object> GetStatusChain(LectionStatus current)
    {
        var order = new[]
        {
            LectionStatus.Open,
            LectionStatus.Started,
            LectionStatus.Finished,
            LectionStatus.Accepted,
            LectionStatus.Rated,
        };

        if (current == LectionStatus.Skipped)
            return new List<object>
            {
                new { status = "skipped", label = LectionStatus.Skipped.DisplayName(), isCurrent = true, isPast = false },
            };

        if (current == LectionStatus.Rejected)
            return new List<object>
            {
                new { status = "open",     label = LectionStatus.Open.DisplayName(),     isCurrent = false, isPast = false },
                new { status = "started",  label = LectionStatus.Started.DisplayName(),  isCurrent = false, isPast = false },
                new { status = "finished", label = LectionStatus.Finished.DisplayName(), isCurrent = false, isPast = false },
                new { status = "rejected", label = LectionStatus.Rejected.DisplayName(), isCurrent = true,  isPast = false },
            };

        var currentIdx = Array.IndexOf(order, current);
        return order
            .Select((s, i) => (object)new
            {
                status = s.ToString().ToLower(),
                label = s.DisplayName(),
                isCurrent = i == currentIdx,
                isPast = i < currentIdx,
            })
            .ToList();
    }

    public void ApplyTransition(LectionProgress progress, LectionStatus to, string? rejectionNote = null)
    {
        progress.Status = to;
        var now = DateTime.UtcNow;

        switch (to)
        {
            case LectionStatus.Started:
                progress.StartedAt = now;
                break;
            case LectionStatus.Finished:
                progress.FinishedAt = now;
                break;
            case LectionStatus.Accepted:
                progress.AcceptedAt = now;
                break;
            case LectionStatus.Rejected:
                progress.RejectedAt = now;
                progress.RejectionNote = rejectionNote;
                break;
            case LectionStatus.Rated:
                progress.RatedAt = now;
                progress.Rating = progress.Rating ?? 3;
                break;
            case LectionStatus.Skipped:
                progress.SkippedAt = now;
                break;
            case LectionStatus.Open:
                progress.StartedAt = null;
                progress.FinishedAt = null;
                progress.RejectionNote = null;
                break;
        }
    }

}
