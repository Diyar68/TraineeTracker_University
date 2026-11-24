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
                Email = "manfred@demo.example",
                PasswordHash = "123",
                Role = "Mentor",
                IsClosed = false
            },
            new User
            {
                Id = "2",
                FirstName = "Hans",
                LastName = "Hilfreich",
                Email = "hans@demo.example",
                PasswordHash = "123",
                Role = "Mentor",
                IsClosed = true
            },
            new User
            {
                Id = "3",
                FirstName = "Admin",
                LastName = "",
                Email = "admin@demo.example",
                PasswordHash = "123",
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

    // Autor: Tarik Polat
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
                Email = "theo@demo.example",
                PasswordHash = "123",
                Role = "Trainee",
                IsClosed = false
            },
            new User
            {
                Id = "11",
                FirstName = "Clara",
                LastName = "Closed",
                Email = "clara@demo.example",
                PasswordHash = "123",
                Role = "Trainee",
                IsClosed = true
            },
            new User
            {
                Id = "12",
                FirstName = "Manfred",
                LastName = "Mental",
                Email = "manfred2@demo.example",
                PasswordHash = "123",
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
            FirstName = "Vanessa",
            LastName = "Vital",
            Email = "vanessa.vital@demo.example",
            PasswordHash = "123",
            Role = "Trainee"
        });

        context.SaveChanges();

        var repository = new UserRepository(context);

        // Act
        var exists = repository.EmailExists("vanessa.vital@demo.example");

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