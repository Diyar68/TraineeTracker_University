using Microsoft.EntityFrameworkCore;
using TraineeTracker.Models;

namespace TraineeTracker.Data;

public static class PortfolioSeed
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        // A fresh, process-owned database is used in demo mode.
        foreach (var (id, first, role) in new[] {
            ("demo-trainee", "Alex", "Trainee"),
            ("demo-trainee-2", "Robin", "Trainee"),
            ("demo-mentor", "Sam", "Mentor") })
            db.Users.Add(new User {
                Id = id, FirstName = first, LastName = "Beispiel",
                Email = id + "@portfolio.example", PasswordHash = "",
                Role = role, TraineeStartDate = DateTime.Today.AddDays(-60),
                TraineeEndDate = DateTime.Today.AddMonths(6)
            });
        foreach (var id in new[] { "demo-trainee", "demo-trainee-2" })
            db.TraineeMentorAssignments.Add(new TraineeMentorAssignment { TraineeId = id, MentorId = "demo-mentor" });
        var titles = new[] { "C# Grundlagen", "Versionsverwaltung mit Git", "HTTP und Webanwendungen",
            "ASP.NET Core MVC", "Datenmodellierung", "Entity Framework Core", "Automatisierte Tests", "Docker Grundlagen" };
        for (var i = 0; i < titles.Length; i++)
        {
            var lesson = new Lection { ExternalId = i + 1, Number = (i + 1).ToString("00"),
                Title = titles[i], Effort = 2 + i % 3, OrderIndex = i };
            db.Lections.Add(lesson);
            foreach (var id in new[] { "demo-trainee", "demo-trainee-2" })
            {
                var progress = new LectionProgress { TraineeId = id, Lection = lesson,
                    Status = i < 3 ? LectionStatus.Rated : i == 3 ? LectionStatus.Started : LectionStatus.Open,
                    StartedAt = i <= 3 ? DateTime.Today.AddDays(-20 + i * 3) : null,
                    FinishedAt = i < 3 ? DateTime.Today.AddDays(-18 + i * 3) : null,
                    AcceptedAt = i < 3 ? DateTime.Today.AddDays(-17 + i * 3) : null,
                    RatedAt = i < 3 ? DateTime.Today.AddDays(-16 + i * 3) : null,
                    Rating = i < 3 ? 3 : null };
                db.LectionProgresses.Add(progress);
                if (i < 3)
                    db.Feedbacks.Add(new Feedback { Progress = progress, DifficultyRating = 2,
                        PriorKnowledge = "Grundkenntnisse aus einer fiktiven Übung",
                        ActualEffortDays = 2, Note = "Das Beispielprojekt hat beim Verständnis geholfen.",
                        CreatedAt = DateTime.Today.AddDays(-16 + i * 3) });
            }
        }
        await db.SaveChangesAsync();
    }
}
