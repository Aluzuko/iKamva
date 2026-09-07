using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<StudentSubject> StudentSubjects => Set<StudentSubject>();

    public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<TutorProfile> TutorProfiles => Set<TutorProfile>();

    public DbSet<TutorSubject> TutorSubjects => Set<TutorSubject>();

    public DbSet<VerificationDocument> VerificationDocuments => Set<VerificationDocument>();

    public DbSet<Dispute> Disputes => Set<Dispute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}