using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using TraineeTracker.Filters;

namespace TraineeTracker.UnitTests;

public class LoginAntiforgeryRecoveryFilterTests
{
    [Theory]
    [InlineData("Account", "Login", "application/x-www-form-urlencoded", true, true)]
    [InlineData("Admin", "Create", "application/x-www-form-urlencoded", true, false)]
    [InlineData("Account", "Login", "application/json", true, false)]
    [InlineData("Account", "Login", "application/x-www-form-urlencoded", false, false)]
    public void RecoversOnlyRejectedLoginForms(string controller, string action, string contentType,
        bool invalidToken, bool shouldRedirect)
    {
        var http = new DefaultHttpContext();
        http.Request.ContentType = contentType;
        var descriptor = new ActionDescriptor();
        descriptor.RouteValues["controller"] = controller;
        descriptor.RouteValues["action"] = action;
        IActionResult original = invalidToken ? new AntiforgeryValidationFailedResult() : new OkResult();
        var context = new ResultExecutingContext(new ActionContext(http, new RouteData(), descriptor),
            new List<IFilterMetadata>(), original, new object());

        new LoginAntiforgeryRecoveryFilter(NullLogger<LoginAntiforgeryRecoveryFilter>.Instance)
            .OnResultExecuting(context);

        if (shouldRedirect)
            Assert.Equal("/Account/Login?sessionExpired=true", Assert.IsType<RedirectResult>(context.Result).Url);
        else
            Assert.Same(original, context.Result);
    }
}
