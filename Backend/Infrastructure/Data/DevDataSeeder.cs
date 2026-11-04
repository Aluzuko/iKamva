using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Data;

public static class DevDataSeeder
{
    private const string DemoPassword = "Demo1234!";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var hash = hasher.Hash(DemoPassword);

        var maria = NewUser("maria@example.com", hash, "Maria Thompson", UserRole.Parent);
        var alex = NewUser("alex@example.com", hash, "Alex Thompson", UserRole.Student);
        var emma = NewUser("emma@example.com", hash, "Emma Thompson", UserRole.Student);
        var sarahChen = NewUser("sarah.chen@example.com", hash, "Dr. Sarah Chen", UserRole.Tutor);
        var jamesOkafor = NewUser("james.okafor@example.com", hash, "James Okafor", UserRole.Tutor);
        var marcusWebb = NewUser("marcus.webb@example.com", hash, "Marcus Webb", UserRole.Tutor);
        var isabelleFontaine = NewUser("isabelle.fontaine@example.com", hash, "Isabelle Fontaine", UserRole.Tutor);

        db.Users.AddRange(maria, alex, emma, sarahChen, jamesOkafor, marcusWebb, isabelleFontaine);

        var alexProfile = new StudentProfile { UserId = alex.Id, YearLevel = "Year 11", LinkCode = "ALEX2026" };
        var emmaProfile = new StudentProfile { UserId = emma.Id, YearLevel = "Year 9", LinkCode = "EMMA2026" };
        db.StudentProfiles.AddRange(alexProfile, emmaProfile);

        db.StudentSubjects.AddRange(
            new StudentSubject { StudentUserId = alex.Id, SubjectId = Configurations.SubjectConfiguration.Mathematics },
            new StudentSubject { StudentUserId = alex.Id, SubjectId = Configurations.SubjectConfiguration.Physics },
            new StudentSubject { StudentUserId = alex.Id, SubjectId = Configurations.SubjectConfiguration.ComputerScience },
            new StudentSubject { StudentUserId = emma.Id, SubjectId = Configurations.SubjectConfiguration.EnglishLiterature },
            new StudentSubject { StudentUserId = emma.Id, SubjectId = Configurations.SubjectConfiguration.French }
        );

        db.ParentStudentLinks.AddRange(
            new ParentStudentLink
            {
                ParentUserId = maria.Id, StudentUserId = alex.Id,
                Relationship = Relationship.Parent, Status = LinkStatus.Active, AcceptedAt = now,
            },
            new ParentStudentLink
            {
                ParentUserId = maria.Id, StudentUserId = emma.Id,
                Relationship = Relationship.Parent, Status = LinkStatus.Active, AcceptedAt = now,
            }
        );

        db.Bookings.AddRange(
            Booking(alex.Id, sarahChen.Id, Configurations.SubjectConfiguration.Mathematics,
                now.AddHours(5), 60, 85m, BookingStatus.Upcoming),
            Booking(alex.Id, marcusWebb.Id, Configurations.SubjectConfiguration.ComputerScience,
                now.AddDays(4).Date.AddHours(15), 60, 90m, BookingStatus.Upcoming),
            Booking(alex.Id, sarahChen.Id, Configurations.SubjectConfiguration.Mathematics,
                now.AddDays(-5), 60, 85m, BookingStatus.Completed,
                "Covered integration by parts and u-substitution. Great session!"),
            Booking(alex.Id, sarahChen.Id, Configurations.SubjectConfiguration.Physics,
                now.AddDays(-7), 90, 128m, BookingStatus.Completed,
                "Electromagnetism and Faraday's law. Practice problems assigned."),
            Booking(alex.Id, sarahChen.Id, Configurations.SubjectConfiguration.Mathematics,
                now.AddDays(-12), 60, 85m, BookingStatus.Cancelled),
            Booking(emma.Id, jamesOkafor.Id, Configurations.SubjectConfiguration.EnglishLiterature,
                now.AddDays(3).Date.AddHours(17), 60, 65m, BookingStatus.Upcoming),
            Booking(emma.Id, jamesOkafor.Id, Configurations.SubjectConfiguration.EnglishLiterature,
                now.AddDays(-9), 60, 65m, BookingStatus.Completed,
                "Essay structure for Hamlet analysis. Strong improvement in thesis writing."),
            Booking(emma.Id, isabelleFontaine.Id, Configurations.SubjectConfiguration.French,
                now.AddDays(-14), 60, 65m, BookingStatus.Completed,
                "Conversational French — restaurant and travel vocabulary.")
        );

        var lastMonth = now.AddMonths(-1);
        var twoMonthsAgo = now.AddMonths(-2);
        var threeMonthsAgo = now.AddMonths(-3);

        db.Invoices.AddRange(
            Invoice(maria.Id, alex.Id, now.Year, now.Month, 8, 620m, InvoiceStatus.Pending),
            Invoice(maria.Id, alex.Id, lastMonth.Year, lastMonth.Month, 10, 780m, InvoiceStatus.Paid),
            Invoice(maria.Id, alex.Id, twoMonthsAgo.Year, twoMonthsAgo.Month, 9, 695m, InvoiceStatus.Paid),
            Invoice(maria.Id, alex.Id, threeMonthsAgo.Year, threeMonthsAgo.Month, 7, 540m, InvoiceStatus.Paid),
            Invoice(maria.Id, emma.Id, now.Year, now.Month, 3, 195m, InvoiceStatus.Paid)
        );

        await db.SaveChangesAsync(ct);
    }

    private static User NewUser(string email, string passwordHash, string fullName, UserRole role) => new()
    {
        Email = email,
        PasswordHash = passwordHash,
        FullName = fullName,
        Role = role,
        IsEmailVerified = true,
    };

    private static Booking Booking(
        Guid studentId, Guid tutorId, Guid subjectId, DateTime scheduledAt, int minutes,
        decimal price, BookingStatus status, string? notes = null) => new()
    {
        StudentUserId = studentId,
        TutorUserId = tutorId,
        SubjectId = subjectId,
        ScheduledAt = scheduledAt,
        DurationMinutes = minutes,
        PriceAmount = price,
        Status = status,
        Notes = notes,
    };

    private static Invoice Invoice(
        Guid parentId, Guid studentId, int year, int month, int lessonCount, decimal amount, InvoiceStatus status) => new()
    {
        ParentUserId = parentId,
        StudentUserId = studentId,
        Year = year,
        Month = month,
        LessonCount = lessonCount,
        AmountTotal = amount,
        Status = status,
        PaidAt = status == InvoiceStatus.Paid ? DateTime.UtcNow : null,
    };
}