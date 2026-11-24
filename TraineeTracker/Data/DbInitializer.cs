using Microsoft.EntityFrameworkCore;
using TraineeTracker.Models;
using TraineeTracker.Services;

namespace TraineeTracker.Data;

// Autor: Tarik Polat
/// <summary>
/// Legt die anfänglichen Daten für die Entwicklung an.
/// </summary>
public static class DbInitializer
{
    private const string StefanId = "seed-trainee-2";
    private const string UrsulaId = "seed-trainee-3";
    private const string ManfredId = "seed-mentor-1";
    private const string HansId = "seed-mentor-2";
    private const string AdminId = "seed-admin";

    private static readonly InitialUser[] InitialUsers =
    [
        new(AdminId, "Morgan", "Example", "admin@demo.example", "AdminDemo!2026", AppConstants.Roles.Admin),
        new(AppConstants.DevTraineeId, "Alex", "Example", "trainee@demo.example", "TraineeDemo!2026", AppConstants.Roles.Trainee, D(2026, 7, 17), D(2027, 3, 31)),
        new(StefanId, "Robin", "Example", "robin@demo.example", "RobinDemo!2026", AppConstants.Roles.Trainee, D(2026, 5, 1), D(2026, 10, 31)),
        new(UrsulaId, "Taylor", "Example", "taylor@demo.example", "TaylorDemo!2026", AppConstants.Roles.Trainee, D(2026, 1, 1), D(2026, 7, 31)),
        new(ManfredId, "Sam", "Example", "mentor@demo.example", "MentorDemo!2026", AppConstants.Roles.Mentor),
        new(HansId, "Casey", "Example", "casey@demo.example", "CaseyDemo!2026", AppConstants.Roles.Mentor),
    ];

    private static readonly (string TraineeId, string MentorId)[] InitialAssignments =
    [
        (AppConstants.DevTraineeId, ManfredId),
        (StefanId, ManfredId),
        (UrsulaId, ManfredId),
        (UrsulaId, HansId),
    ];

    private static readonly string[] UrsulaFeedbackNotes =
    [
        "The practice task clarified the topic.",
        "I used a small fictional sample project.",
        "The exercises helped me apply the concept.",
        "I would like to explore the topic further.",
    ];

    // Autor: Tarik Polat
    /// <summary>
    /// Führt alle Schritte zum Anlegen der Startdaten in der richtigen Reihenfolge aus.
    /// </summary>
    /// <param name="app">Die Web-Anwendung mit den registrierten Services.</param>
    public static async Task SeedAsync(AppDbContext context)
    {
        var parser = new CurriculumJsonParser();
        var syncService = new TraineeTracker.Data.Repositories.DatabaseLectionRepository(context);

        await SeedCurriculumAsync(parser, syncService);
        await SeedUsersAsync(context);
        await SeedAssignmentsAsync(context);
        await SeedProgressAndFeedbackAsync(context);
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Importiert den anfänglichen Lehrplan aus der JSON-Datei.
    /// </summary>
    /// <param name="parser">Parser für die Lehrplan-JSON-Datei.</param>
    /// <param name="syncService">Service zum Einspielen des Lehrplans in die Datenbank.</param>
    private static async Task SeedCurriculumAsync(
        ICurriculumJsonParser parser,
        ICurriculumSyncService syncService)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "lehrplan_devop.json");
        await using var stream = File.OpenRead(path);
        var lections = await parser.ParseAsync(stream);
        await syncService.ImportAsync(1, lections);
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Legt die anfänglichen Benutzer an oder aktualisiert sie.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    private static async Task SeedUsersAsync(AppDbContext context)
    {
        foreach (var initialUser in InitialUsers)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == initialUser.Id);
            if (user == null)
            {
                context.Users.Add(new User
                {
                    Id = initialUser.Id,
                    FirstName = initialUser.FirstName,
                    LastName = initialUser.LastName,
                    Email = initialUser.Email,
                    PasswordHash = PasswordHasher.Hash(initialUser.Password),
                    Role = initialUser.Role,
                    TraineeStartDate = initialUser.TraineeStartDate,
                    TraineeEndDate = initialUser.TraineeEndDate,
                });
                continue;
            }

            user.FirstName = initialUser.FirstName;
            user.LastName = initialUser.LastName;
            user.Email = initialUser.Email;
            user.Role = initialUser.Role;
            user.TraineeStartDate = initialUser.TraineeStartDate;
            user.TraineeEndDate = initialUser.TraineeEndDate;

            if (!PasswordHasher.Verify(initialUser.Password, user.PasswordHash))
                user.PasswordHash = PasswordHasher.Hash(initialUser.Password);
        }

        await context.SaveChangesAsync();
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Legt die anfänglichen Mentor-Trainee-Zuordnungen an.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    private static async Task SeedAssignmentsAsync(AppDbContext context)
    {
        foreach (var assignment in InitialAssignments)
        {
            var exists = await context.TraineeMentorAssignments.AnyAsync(a =>
                a.TraineeId == assignment.TraineeId && a.MentorId == assignment.MentorId);

            if (!exists)
            {
                context.TraineeMentorAssignments.Add(new TraineeMentorAssignment
                {
                    TraineeId = assignment.TraineeId,
                    MentorId = assignment.MentorId,
                });
            }
        }

        await context.SaveChangesAsync();
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Legt den anfänglichen Lernfortschritt und das anfängliche Feedback an.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    private static async Task SeedProgressAndFeedbackAsync(AppDbContext context)
    {
        var lections = await context.Lections
            .Where(l => l.IsActive)
            .OrderBy(l => l.OrderIndex < 0 ? int.MaxValue : l.OrderIndex)
            .ThenBy(l => l.Title)
            .ThenBy(l => l.Id)
            .ToListAsync();

        if (lections.Count == 0)
            return;

        await SeedTraineeProgressAsync(
            context,
            AppConstants.DevTraineeId,
            lections,
            0,
            LectionStatus.Open,
            D(2026, 7, 17),
            1,
            false);

        await SeedTraineeProgressAsync(
            context,
            StefanId,
            lections,
            CountForPercentage(lections.Count, 30),
            LectionStatus.Finished,
            D(2026, 5, 1),
            4,
            false);

        await SeedTraineeProgressAsync(
            context,
            UrsulaId,
            lections,
            CountForPercentage(lections.Count, 90),
            LectionStatus.Rated,
            D(2026, 1, 1),
            3,
            true);
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Legt Lernfortschritt und optional Feedback für einen Trainee an.
    /// </summary>
    /// <param name="context">Der Datenbankkontext.</param>
    /// <param name="traineeId">Id des Trainees.</param>
    /// <param name="lections">Liste der aktiven Lektionen.</param>
    /// <param name="transitionedCount">Anzahl der Lektionen, die auf den Zielstatus gesetzt werden.</param>
    /// <param name="transitionedStatus">Zielstatus für die transitionierten Lektionen.</param>
    /// <param name="firstStartedAt">Startdatum der ersten Lektion.</param>
    /// <param name="daysBetweenStarts">Abstand in Tagen zwischen den Startdaten der Lektionen.</param>
    /// <param name="createFeedback">Gibt an, ob auch Feedback angelegt werden soll.</param>
    private static async Task SeedTraineeProgressAsync(
        AppDbContext context,
        string traineeId,
        IReadOnlyList<Lection> lections,
        int transitionedCount,
        LectionStatus transitionedStatus,
        DateTime firstStartedAt,
        int daysBetweenStarts,
        bool createFeedback)
    {
        if (await context.LectionProgresses.AnyAsync(p => p.TraineeId == traineeId))
            return;

        var progressed = new List<LectionProgress>();

        for (var index = 0; index < lections.Count; index++)
        {
            var status = index < transitionedCount ? transitionedStatus : LectionStatus.Open;
            var progress = CreateProgress(
                traineeId,
                lections[index].Id,
                status,
                firstStartedAt.AddDays(index * daysBetweenStarts));

            context.LectionProgresses.Add(progress);

            if (status == LectionStatus.Rated)
                progressed.Add(progress);
        }

        await context.SaveChangesAsync();

        if (!createFeedback)
            return;

        for (var index = 0; index < progressed.Count; index++)
        {
            context.Feedbacks.Add(new Feedback
            {
                LectionProgressId = progressed[index].Id,
                DifficultyRating = 0,
                PriorKnowledge = "",
                ActualEffortDays = 0,
                Note = index < UrsulaFeedbackNotes.Length ? UrsulaFeedbackNotes[index] : null,
                CreatedAt = progressed[index].RatedAt!.Value,
            });
        }

        await context.SaveChangesAsync();
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Legt einen Lernfortschritt mit passenden Zeitstempeln an.
    /// </summary>
    /// <param name="traineeId">Id des Trainees.</param>
    /// <param name="lectionId">Id der Lektion.</param>
    /// <param name="status">Status des Lernfortschritts.</param>
    /// <param name="startedAt">Startdatum der Lektion.</param>
    private static LectionProgress CreateProgress(
        string traineeId,
        int lectionId,
        LectionStatus status,
        DateTime startedAt)
    {
        var progress = new LectionProgress
        {
            TraineeId = traineeId,
            LectionId = lectionId,
            Status = status,
        };

        if (status == LectionStatus.Open)
            return progress;

        progress.StartedAt = startedAt;
        progress.FinishedAt = startedAt.AddDays(1);

        if (status == LectionStatus.Rated)
        {
            progress.AcceptedAt = startedAt.AddDays(2);
            progress.RatedAt = startedAt.AddDays(3);
            progress.Rating = 4;
        }

        return progress;
    }

    // Autor: Tarik Polat
    /// <summary>
    /// Berechnet die gerundete Anzahl an Einträgen für einen Prozentsatz.
    /// </summary>
    /// <param name="total">Gesamtanzahl der Einträge.</param>
    /// <param name="percentage">Gewünschter Prozentsatz.</param>
    private static int CountForPercentage(int total, int percentage) =>
        (int)Math.Round(total * percentage / 100m, MidpointRounding.AwayFromZero);

    // Autor: Tarik Polat
    /// <summary>
    /// Erstellt ein Datum um Mitternacht.
    /// </summary>
    /// <param name="year">Jahr des Datums.</param>
    /// <param name="month">Monat des Datums.</param>
    /// <param name="day">Tag des Datums.</param>
    private static DateTime D(int year, int month, int day) =>
        new DateTime(year, month, day).AddDays((DateTime.Today.AddDays(-60) - new DateTime(2026, 7, 17)).Days);

    private sealed record InitialUser(
        string Id,
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string Role,
        DateTime? TraineeStartDate = null,
        DateTime? TraineeEndDate = null);
}
