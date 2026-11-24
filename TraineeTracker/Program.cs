using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TraineeTracker;
using TraineeTracker.Data;
using TraineeTracker.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
if (!int.TryParse(port, out var portNumber) || portNumber < 1 || portNumber > 65535)
    throw new InvalidOperationException("PORT must be a valid TCP port.");
builder.WebHost.UseUrls($"http://0.0.0.0:{portNumber}");

// ═══════════════════════════════════════════════════════
// SERVICES REGISTRIEREN
// ═══════════════════════════════════════════════════════

var demo = new TraineeTracker.Services.DemoMode();
if (!demo.Enabled)
    throw new InvalidOperationException("Set Demo__Enabled=true to run this portfolio demo.");
builder.Services.AddSingleton(demo);
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Anmeldung: signiertes Cookie, das der Server ausstellt und prüft.
// (Das separate, frei lesbare "role"-Cookie dient nur noch der Anzeige in den Views.)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = AppConstants.Cookies.Authentication;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;

        // Geschlossene Konten sofort aussperren, auch wenn noch ein gültiges Cookie vorliegt.
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var userRepository = context.HttpContext.RequestServices.GetRequiredService<UserRepository>();
                var user = userRepository.GetById(userId);

                if (user == null || user.IsClosed || (demo.Enabled && user.Id is not ("demo-trainee" or "demo-mentor")))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

// Gemeinsame Anwendungsdatenbank
var dbPath = demo.Enabled ? Path.Combine(Path.GetTempPath(), "traineetracker-demo", Guid.NewGuid().ToString("N"), "demo.db") : builder.Configuration["Database:Path"] ??
    Path.Combine(builder.Environment.ContentRootPath, "data", "persistence", "traineetracker.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Repositories
builder.Services.AddScoped<ILectionRepository, DatabaseLectionRepository>();
builder.Services.AddScoped<ILectionProgressRepository, DatabaseLectionProgressRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<MentorAssignmentRepository>();
builder.Services.AddScoped<FeedbackRepository>();

// Services
builder.Services.AddScoped<TraineeTracker.Services.ICurriculumJsonParser, TraineeTracker.Services.CurriculumJsonParser>();
builder.Services.AddScoped<TraineeTracker.Services.ICurriculumSyncService>(sp =>
    (TraineeTracker.Services.ICurriculumSyncService)sp.GetRequiredService<ILectionRepository>());
builder.Services.AddScoped<TraineeTracker.Services.ILectionTransitionService, TraineeTracker.Services.LectionTransitionService>();
builder.Services.AddScoped<TraineeTracker.Services.DashboardService>();
builder.Services.AddScoped<TraineeTracker.Services.DashboardGroupingService>();
builder.Services.AddScoped<TraineeTracker.Services.ProgressStatsService>();
builder.Services.AddScoped<TraineeTracker.Services.WeeklyPlanService>();
builder.Services.AddScoped<TraineeTracker.Services.ICurrentUserService, TraineeTracker.Services.DemoCurrentUserService>();
builder.Services.AddScoped<TraineeTracker.Services.IDemoRoleService, TraineeTracker.Services.DemoRoleService>();
builder.Services.AddScoped<TraineeTracker.Services.DemoFeedbackService>();
builder.Services.AddScoped<TraineeTracker.Services.MentorDashboardService>();

builder.Services.AddScoped<TraineeTracker.Services.IAttendanceService,
    TraineeTracker.Services.SyntheticAttendanceService>();

// ═══════════════════════════════════════════════════════
// PIPELINE KONFIGURIEREN
// ═══════════════════════════════════════════════════════

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/TraineeDashboard/Index");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment() && Environment.GetEnvironmentVariable("RENDER") != "true")
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (demo.Enabled)
    {
        var controller = context.GetRouteValue("controller")?.ToString();
        var action = context.GetRouteValue("action")?.ToString();
        var sessionAction = (controller == "Account" && action is "DemoLogin" or "Logout")
            || (controller == "Mentor" && action == "SelectTrainee");
        if (controller is "Admin" or "CurriculumImport"
            || (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !sessionAction))
        {
            context.Response.StatusCode = 403;
            return;
        }
    }
    await next();
});
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHealthChecks("/healthz");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TraineeDashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

// ═══════════════════════════════════════════════════════
// MIGRATION / SEED
// ═══════════════════════════════════════════════════════

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (demo.Enabled)
{
    using var scope = app.Services.CreateScope();
    await PortfolioSeed.InitializeAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}


app.Run();
