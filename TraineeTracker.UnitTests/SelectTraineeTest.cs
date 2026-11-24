using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TraineeTracker.Data;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraineeTracker.Controllers;

namespace TraineeTracker.UnitTests;


public class MentorControllerTests
{
    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Fact]
    public void SelectTrainee_SetsCookieAndRedirectsToDashboard()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<UserRepository>();
        services.AddScoped<MentorAssignmentRepository>();
        services.AddSingleton<IWebHostEnvironment>(new TestEnvironment());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new User { Id = "trainee-42", FirstName = "Demo", LastName = "Beispiel",
            Email = "demo@example.test", PasswordHash = "", Role = "Trainee" });
        db.TraineeMentorAssignments.Add(new TraineeMentorAssignment { TraineeId = "trainee-42", MentorId = "mentor" });
        db.SaveChanges();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider,
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, "mentor"), new Claim(ClaimTypes.Role, "Mentor")
            }, "test")) };
        var controller = new MentorController()
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            Url = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(new ActionContext(context, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()))
        };

        var result = controller.SelectTrainee("trainee-42");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("TraineeDashboard", redirect.ControllerName);

        var cookie = context.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("traineeTracker.mentorTrainee", cookie);
        Assert.Contains("trainee-42", cookie);
    }

    [Fact]
    public void SelectTrainee_ReturnsBadRequest_WhenIdIsMissing()
    {
        var controller = new MentorController()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.SelectTrainee("   ");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("TraineeId erforderlich", badRequest.Value);
    }
}
