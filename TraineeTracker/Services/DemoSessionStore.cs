using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TraineeTracker.Data;

namespace TraineeTracker.Services;

/// <summary>Owns disposable, isolated databases. No visitor controls a filesystem path.</summary>
public sealed class DemoSessionStore
{
    public const string CookieName = "TraineeTracker.Sandbox";
    public const string ItemName = "DemoSession";
    private readonly object _sync = new();
    private readonly Dictionary<string, Session> _sessions = new();
    private readonly IDataProtector _protector;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "trainee-sandboxes", Guid.NewGuid().ToString("N"));
    public string TemplatePath => Path.Combine(_root, "template.db");
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(2);

    public DemoSessionStore(IDataProtectionProvider protection)
    {
        _protector = protection.CreateProtector("TraineeTracker.Sandbox.v1");
        Directory.CreateDirectory(_root);
    }

    public async Task InitializeAsync()
    {
        await using var context = new AppDbContext(Options(TemplatePath));
        await context.Database.MigrateAsync();
        await DbInitializer.SeedAsync(context);
    }

    public static DbContextOptions<AppDbContext> Options(string path) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()).Options;

    public Session? Acquire(HttpContext context)
    {
        lock (_sync)
        {
            // Active requests hold a lease, so cleanup cannot delete their database.
            foreach (var expired in _sessions.Values.Where(s => s.Expires <= DateTimeOffset.UtcNow && s.Leases == 0).ToList())
            {
                _sessions.Remove(expired.Id);
                foreach (var path in Directory.EnumerateFiles(_root, expired.Id + ".db*"))
                    File.Delete(path);
            }

            string? id = null;
            if (context.Request.Cookies.TryGetValue(CookieName, out var cookie))
            {
                try { id = _protector.Unprotect(cookie); }
                catch (CryptographicException) { /* Invalid or old cookies receive a fresh sandbox. */ }
            }
            if (id != null && _sessions.TryGetValue(id, out var existing) && existing.Expires > DateTimeOffset.UtcNow)
            {
                existing.Leases++;
                return existing;
            }
            if (_sessions.Count >= 64)
                return null;

            id = Guid.NewGuid().ToString("N");
            var session = new Session(id, Path.Combine(_root, id + ".db"), DateTimeOffset.UtcNow.Add(Lifetime));
            using (var source = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource = TemplatePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
            using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource = session.Path, Pooling = false }.ToString()))
            {
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);
            }
            _sessions.Add(id, session);
            session.Leases++;
            context.Response.Cookies.Append(CookieName, _protector.Protect(id), new CookieOptions {
                HttpOnly = true, Secure = !context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
                SameSite = SameSiteMode.Lax, MaxAge = Lifetime, IsEssential = true
            });
            return session;
        }
    }

    public void Release(Session session)
    {
        lock (_sync) session.Leases--;
    }

    public sealed class Session(string id, string path, DateTimeOffset expires)
    {
        public string Id { get; } = id;
        public string Path { get; } = path;
        public DateTimeOffset Expires { get; set; } = expires;
        public int Leases { get; set; }
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}
