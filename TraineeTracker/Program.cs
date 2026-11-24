using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using TraineeTracker;
using TraineeTracker.Data;
using TraineeTracker.Data.Repositories;
using TraineeTracker.Services;

var builder = WebApplication.CreateBuilder(args);
if (!string.Equals(Environment.GetEnvironmentVariable("Demo__Enabled"), "true", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("This isolated deployment requires Demo__Enabled=true.");

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
if (!int.TryParse(port, out var portNumber) || portNumber is < 1 or > 65535)
    throw new InvalidOperationException("PORT must be a valid TCP port.");
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 2 * 1024 * 1024);
builder.WebHost.UseUrls($"http://0.0.0.0:{portNumber}");
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<TraineeTracker.Filters.LoginAntiforgeryRecoveryFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
builder.Services.AddSingleton<DemoSessionStore>();
builder.Services.AddHealthChecks();
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions {
                PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
            }));
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = AppConstants.Cookies.Authentication;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = DemoSessionStore.Lifetime;
    options.Events.OnValidatePrincipal = async context =>
    {
        if (context.HttpContext.Items[DemoSessionStore.ItemName] is not DemoSessionStore.Session session)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }
        var user = context.HttpContext.RequestServices.GetRequiredService<UserRepository>()
            .GetById(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier));
        if (user == null || user.IsClosed || user.Role != context.Principal?.FindFirstValue(ClaimTypes.Role)
            || session.Id != context.Principal?.FindFirstValue("sandbox"))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    };
});
builder.Services.AddScoped(sp =>
{
    var http = sp.GetRequiredService<IHttpContextAccessor>().HttpContext!;
    var session = (DemoSessionStore.Session)http.Items[DemoSessionStore.ItemName]!;
    return new AppDbContext(DemoSessionStore.Options(session.Path));
});
builder.Services.AddScoped<ILectionRepository, DatabaseLectionRepository>();
builder.Services.AddScoped<ILectionProgressRepository, DatabaseLectionProgressRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<MentorAssignmentRepository>();
builder.Services.AddScoped<FeedbackRepository>();
builder.Services.AddScoped<ICurriculumJsonParser, CurriculumJsonParser>();
builder.Services.AddScoped<ICurriculumSyncService>(sp => (ICurriculumSyncService)sp.GetRequiredService<ILectionRepository>());
builder.Services.AddScoped<ILectionTransitionService, LectionTransitionService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<DashboardGroupingService>();
builder.Services.AddScoped<ProgressStatsService>();
builder.Services.AddScoped<WeeklyPlanService>();
builder.Services.AddScoped<ICurrentUserService, DemoCurrentUserService>();
builder.Services.AddScoped<IDemoRoleService, DemoRoleService>();
builder.Services.AddScoped<DemoFeedbackService>();
builder.Services.AddScoped<MentorDashboardService>();
builder.Services.AddScoped<IAttendanceService, SyntheticAttendanceService>();

var app = builder.Build();
await app.Services.GetRequiredService<DemoSessionStore>().InitializeAsync();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/Error");
    app.UseHsts();
    // Render terminates TLS and redirects HTTP at its edge. No untrusted forwarded headers are consumed.
    if (Environment.GetEnvironmentVariable("RENDER") != "true")
        app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.GetEndpoint()?.DisplayName == "Health checks")
    {
        await context.GetEndpoint()!.RequestDelegate!(context);
        return;
    }
    if (context.GetEndpoint() == null)
    {
        context.Response.StatusCode = 404;
        return;
    }
    var store = context.RequestServices.GetRequiredService<DemoSessionStore>();
    var session = store.Acquire(context);
    if (session == null)
    {
        context.Response.StatusCode = 503;
        await context.Response.WriteAsync("All demo sandboxes are currently in use. Please try again later.");
        return;
    }
    var entered = false;
    try
    {
        await session.Gate.WaitAsync(context.RequestAborted);
        entered = true;
        context.Items[DemoSessionStore.ItemName] = session;
        context.Response.Headers.CacheControl = "no-store";
        await next();
    }
    finally
    {
        if (entered) session.Gate.Release();
        store.Release(session);
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapHealthChecks("/healthz");
app.MapControllerRoute("default", "{controller=TraineeDashboard}/{action=Index}/{id?}").WithStaticAssets();
app.Run();
