namespace TraineeTracker.ViewModels;

// Autor: Tarik Polat
/// <summary>
/// Ein Feedback-Eintrag für die Anzeige in den Feedback-Listen.
/// </summary>
public class FeedbackEntryViewModel
{
    /// <summary>Id des Feedbacks.</summary>
    public int Id { get; set; }

    /// <summary>Id der zugehörigen Lektion.</summary>
    public int LectionId { get; set; }

    /// <summary>Titel der Lektion.</summary>
    public string LessonTitle { get; set; } = "";

    /// <summary>Name des Trainees.</summary>
    public string TraineeName { get; set; } = "";

    /// <summary>Zeitpunkt, zu dem das Feedback abgegeben wurde.</summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>Schwierigkeit der Lektion als Text.</summary>
    public string Difficulty { get; set; } = "";

    /// <summary>Vorkenntnisse des Trainees.</summary>
    public string PriorKnowledge { get; set; } = "";

    /// <summary>Tatsächlicher Aufwand in Personentagen.</summary>
    public decimal ActualEffortDays { get; set; }

    /// <summary>Anmerkung zum Feedback.</summary>
    public string Comment { get; set; } = "";

    /// <summary>Gibt an, ob es das eigene Feedback des angemeldeten Trainees ist.</summary>
    public bool IsOwnFeedback { get; set; }
}

// Autor: Tarik Polat
/// <summary>
/// Daten aus dem AJAX-Formular zum Anlegen eines Feedbacks.
/// </summary>
public class DashboardFeedbackCreateRequest
{
    /// <summary>Id der Lektion, für die Feedback abgegeben wird.</summary>
    public int LectionId { get; set; }

    /// <summary>Schwierigkeit der Lektion.</summary>
    public string? Difficulty { get; set; }

    /// <summary>Vorkenntnisse des Trainees.</summary>
    public string? PriorKnowledge { get; set; }

    /// <summary>Tatsächlicher Aufwand in Personentagen.</summary>
    public decimal? Effort { get; set; }

    /// <summary>Anmerkung zum Feedback.</summary>
    public string? Comment { get; set; }
}

// Autor: Tarik Polat
/// <summary>
/// Daten aus dem AJAX-Formular zum Ändern eines Feedbacks.
/// </summary>
public class DashboardFeedbackUpdateRequest
{
    /// <summary>Schwierigkeit der Lektion.</summary>
    public string? Difficulty { get; set; }

    /// <summary>Vorkenntnisse des Trainees.</summary>
    public string? PriorKnowledge { get; set; }

    /// <summary>Tatsächlicher Aufwand in Personentagen.</summary>
    public decimal? Effort { get; set; }

    /// <summary>Anmerkung zum Feedback.</summary>
    public string? Comment { get; set; }
}

// Autor: Tarik Polat
/// <summary>Data needed by the dashboard after feedback is deleted.</summary>
public class FeedbackDeletionResult
{
    /// <summary>Id der Lektion.</summary>
    public int LectionId { get; set; }

    /// <summary>Titel der Lektion.</summary>
    public string LessonTitle { get; set; } = "";

}
