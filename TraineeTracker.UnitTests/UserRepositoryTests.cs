using Microsoft.EntityFrameworkCore;
using TraineeTracker.Data;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using Xunit;

namespace TraineeTracker.UnitTests;

public class UserRepositoryTests
{
    [Fact]
    public void GetMentors_ShouldReturnOnlyActiveMentors()
    {
        // Arrange
        using var context = CreateContext();

        context.Users.AddRange(
            new User
            {
                Id = "1",
                FirstName = "Manfred",
                LastName = "Mental",
                Email = "manfred@portfolio.example",
                PasswordHash = "fixture-unused-hash",
                Role = "Mentor",
                IsClosed = false
            },
            new User
            {
                Id = "2",
                FirstName = "Hans",
                LastName = "Hilfreich",
                Email = "hans@portfolio.example",
                PasswordHash = "fixture-unused-hash",
                Role = "Mentor",
                IsClosed = true
            },
            new User
            {
                Id = "3",
                FirstName = "Admin",
                LastName = "",
                Email = "admin@portfolio.example",
                PasswordHash = "fixture-unused-hash",
                Role = "Admin",
                IsClosed = false
            });

        context.SaveChanges();

        var repository = new UserRepository(context);

        // Act
        var mentors = repository.GetMentors();

        // Assert
        Assert.Single(mentors);
        Assert.Equal("Manfred", mentors[0].FirstName);
    }

    /// <summary>
    /// Prüft, dass GetTrainees() gesperrte Trainee-Konten nicht zurückgibt.
    /// </summary>
    [Fact]
    public void GetTrainees_ExcludesClosed()
    {
        // Arrange
        using var context = CreateContext();

        context.Users.AddRange(
            new User
            {
                Id = "10",
                FirstName = "Theo",
                LastName = "Trainee",
                Email = "theo@portfolio.example",
                PasswordHash = "fixture-unused-hash",
                Role = "Trainee",
                IsClosed = false
            },
            new User
            {
                Id = "11",
                FirstName = "Clara",
                LastName = "Closed",
                Email = "clara@portfolio.example",
                PasswordHash = "fixture-unused-hash",
                Role = "Trainee",
                IsClosed = true
            },
            new User
            {
                Id = "12",
                FirstName = "Manfred",
                LastName = "Mental",
                Email = "manfred2@portfolio.example",
                PasswordHash = "fixture-unused-hash",
                Role = "Mentor",
                IsClosed = false
            });

        context.SaveChanges();

        var repository = new UserRepository(context);

        // Act
        var trainees = repository.GetTrainees();

        // Assert
        Assert.Single(trainees);
        Assert.Equal("Theo", trainees[0].FirstName);
    }

    [Fact]
    public void EmailExists_ShouldReturnTrue_WhenEmailExists()
    {
        // Arrange
        using var context = CreateContext();

        context.Users.Add(new User
        {
            Id = "1",
            FirstName = "Alex",
            LastName = "Beispiel",
            Email = "alex.beispiel@portfolio.example",
            PasswordHash = "fixture-unused-hash",
            Role = "Trainee"
        });

        context.SaveChanges();

        var repository = new UserRepository(context);

        // Act
        var exists = repository.EmailExists("alex.beispiel@portfolio.example");

        // Assert
        Assert.True(exists);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}