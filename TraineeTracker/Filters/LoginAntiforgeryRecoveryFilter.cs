using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TraineeTracker.Filters;

/// <summary>Rejects stale login submissions and obtains a new form without replaying credentials.</summary>
public sealed class LoginAntiforgeryRecoveryFilter(ILogger<LoginAntiforgeryRecoveryFilter> logger) : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        var route = context.ActionDescriptor.RouteValues;
        route.TryGetValue("controller", out var controller);
        route.TryGetValue("action", out var action);
        if (context.Result is not IAntiforgeryValidationFailedResult
            || !context.HttpContext.Request.HasFormContentType
            || controller != "Account"
            || action != "Login")
            return;

        logger.LogWarning("Login antiforgery validation failed. Request {RequestId}. A fresh login form is required.",
            context.HttpContext.TraceIdentifier);
        // Antiforgery has already prevented the action from running. The next request is a safe GET.
        context.Result = new RedirectResult("/Account/Login?sessionExpired=true");
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
