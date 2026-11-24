using Microsoft.EntityFrameworkCore;
using TraineeTracker.Models;

namespace TraineeTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Lection>(e =>
        {
            e.HasKey(l => l.Id);

            e.Property(l => l.Id)
             .ValueGeneratedOnAdd();

            e.Property(l => l.ExternalId)
             .IsRequired();

            e.Property(l => l.Number)
             .IsRequired()
             .HasMaxLength(10);

            e.Property(l => l.Title)
             .IsRequired()
             .HasMaxLength(200);

            e.Property(l => l.Effort);
            e.Property(l => l.OrderIndex);

            e.Property(l => l.IsActive)
             .HasDefaultValue(true);

            e.Ignore(l => l.IsDeprecated);

            e.Property(l => l.ExternalLink)
             .HasMaxLength(500);

            e.HasIndex(l => l.Number);
            e.HasIndex(l => l.ExternalId)
             .IsUnique();
        });

        modelBuilder.Entity<LectionProgress>(e =>
        {
            e.HasKey(p => p.Id);

            e.HasIndex(p => new { p.TraineeId, p.LectionId })
             .IsUnique();

            e.Property(p => p.TraineeId)
             .IsRequired()
             .HasMaxLength(450);

            e.Property(p => p.Status)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.Property(p => p.RejectionNote)
             .HasMaxLength(1000);

            e.Property(p => p.IndividualOrderIndex);

            e.HasOne(p => p.Lection)
             .WithMany(l => l.Progresses)
             .HasForeignKey(p => p.LectionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);

            e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            e.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).IsRequired().HasMaxLength(20);

            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<TraineeMentorAssignment>(e =>
        {
            e.HasKey(a => new { a.TraineeId, a.MentorId });
        });

        modelBuilder.Entity<Feedback>(e =>
        {
            e.HasKey(f => f.Id);

            e.Property(f => f.PriorKnowledge).HasMaxLength(1000);
            e.Property(f => f.Note).HasMaxLength(2000);

            e.HasIndex(f => f.LectionProgressId).IsUnique();

            e.HasOne(f => f.Progress)
             .WithMany()
             .HasForeignKey(f => f.LectionProgressId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public DbSet<Lection> Lections { get; set; }
    public DbSet<LectionProgress> LectionProgresses { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<TraineeMentorAssignment> TraineeMentorAssignments { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
}
