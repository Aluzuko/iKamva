using System.Text;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Storage;
using Backend.Infrastructure.Tutors;
using Microsoft.EntityFrameworkCore;
using Subjects = Backend.Infrastructure.Data.Configurations.SubjectConfiguration;

namespace Backend.Infrastructure.Data;

public static class TutorModuleSeeder
{
    public static async Task SeedAsync(AppDbContext db, IFileStorage storage, CancellationToken ct = default)
    {
        await BackfillCommissionAsync(db, ct);

        var users = await db.Users
            .Where(u => u.Email == "sarah.chen@example.com" || u.Email == "james.okafor@example.com"
                || u.Email == "marcus.webb@example.com" || u.Email == "isabelle.fontaine@example.com"
                || u.Email == "alex@example.com" || u.Email == "emma@example.com")
            .ToDictionaryAsync(u => u.Email, ct);

        if (!users.TryGetValue("sarah.chen@example.com", out var sarah)
            || !users.TryGetValue("alex@example.com", out var alex)
            || !users.TryGetValue("emma@example.com", out var emma))
        {
            return;
        }

        users.TryGetValue("james.okafor@example.com", out var james);
        users.TryGetValue("marcus.webb@example.com", out var marcus);
        users.TryGetValue("isabelle.fontaine@example.com", out var isabelle);

        if (!await db.TutorProfiles.AnyAsync(t => t.UserId == sarah.Id, ct))
        {
            await AddProfileAsync(db, storage, sarah.Id, VerificationStatus.Approved, DateTime.UtcNow.AddDays(-60), 85m, 12,
                "PhD in applied mathematics. I help senior-phase learners turn maths and physics from scary to logical, one worked example at a time.",
                [Subjects.Mathematics, Subjects.Physics], [DocumentType.IdDocument, DocumentType.Qualification, DocumentType.BackgroundCheck], ct);
            AddAvailability(db, sarah.Id, [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday], 15 * 60, 19 * 60);
            AddAvailability(db, sarah.Id, [DayOfWeek.Saturday], 9 * 60, 12 * 60);
        }

        if (james is not null && !await db.TutorProfiles.AnyAsync(t => t.UserId == james.Id, ct))
        {
            await AddProfileAsync(db, storage, james.Id, VerificationStatus.Approved, DateTime.UtcNow.AddDays(-45), 65m, 6,
                "English teacher focused on essay writing and literature analysis.",
                [Subjects.EnglishLiterature], [DocumentType.IdDocument, DocumentType.Qualification, DocumentType.BackgroundCheck], ct);
            AddAvailability(db, james.Id, [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday], 16 * 60, 20 * 60);
        }

        if (marcus is not null && !await db.TutorProfiles.AnyAsync(t => t.UserId == marcus.Id, ct))
        {
            await AddProfileAsync(db, storage, marcus.Id, VerificationStatus.Pending, DateTime.UtcNow.AddDays(-1), 90m, 8,
                "Software engineer teaching programming and computer science fundamentals.",
                [Subjects.ComputerScience], [DocumentType.IdDocument, DocumentType.Qualification, DocumentType.BackgroundCheck], ct);
        }

        if (isabelle is not null && !await db.TutorProfiles.AnyAsync(t => t.UserId == isabelle.Id, ct))
        {
            await AddProfileAsync(db, storage, isabelle.Id, VerificationStatus.Pending, null, 65m, 4,
                null, [Subjects.French], [DocumentType.IdDocument], ct);
        }

        await db.SaveChangesAsync(ct);
        await SeedConversationsAsync(db, sarah.Id, alex.Id, ct);

        if (await db.LessonReviews.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var today = TutorRules.ToLocal(now).Date;
        DateTime Local(DateTime day, int hour) => TutorRules.LocalToUtc(day.AddHours(hour));

        var extra = new List<Booking>
        {
            NewBooking(alex.Id, sarah.Id, Subjects.Mathematics, Local(today, 16), 60, 85m, BookingStatus.Upcoming),
            NewBooking(emma.Id, sarah.Id, Subjects.Mathematics, Local(today, 18), 60, 85m, BookingStatus.Upcoming),
            NewBooking(alex.Id, sarah.Id, Subjects.Physics, Local(today.AddDays(2), 15), 90, 128m, BookingStatus.Upcoming),
            NewBooking(emma.Id, sarah.Id, Subjects.Mathematics, Local(today.AddDays(-1), 17), 60, 85m, BookingStatus.Upcoming),
            NewBooking(alex.Id, sarah.Id, Subjects.Mathematics, Local(today.AddDays(-3), 16), 60, 85m, BookingStatus.Completed),
            NewBooking(emma.Id, sarah.Id, Subjects.Mathematics, Local(today.AddDays(-20), 16), 60, 85m, BookingStatus.Completed,
                "Fractions to decimals, then a timed practice set. Emma finished 18 of 20."),
        };
        for (var month = 1; month <= 5; month++)
        {
            for (var lesson = 0; lesson < 3 + month % 3; lesson++)
            {
                extra.Add(NewBooking(alex.Id, sarah.Id, lesson % 2 == 0 ? Subjects.Mathematics : Subjects.Physics,
                    Local(today.AddMonths(-month).AddDays(-lesson * 3), 16), 60, lesson % 2 == 0 ? 85m : 90m, BookingStatus.Completed,
                    "Worked through past-paper questions and reviewed mistakes together."));
            }
        }
        db.Bookings.AddRange(extra);
        await db.SaveChangesAsync(ct);
        await BackfillCommissionAsync(db, ct);

        var comments = new[]
        {
            "Explains things so clearly. Integration finally makes sense!",
            "Patient and well prepared. Would book again.",
            "Good session, but we ran a few minutes over.",
            null,
            "Really helpful before my test.",
            "Great examples, a bit fast in places.",
        };
        var sarahId = sarah.Id;
        var jamesId = james?.Id ?? Guid.Empty;
        var completed = await db.Bookings
            .Where(b => b.Status == BookingStatus.Completed && (b.TutorUserId == sarahId || b.TutorUserId == jamesId))
            .OrderByDescending(b => b.ScheduledAt)
            .Take(12)
            .ToListAsync(ct);
        for (var i = 0; i < completed.Count; i++)
        {
            if (i == 1)
            {
                continue;
            }
            var b = completed[i];
            db.LessonReviews.Add(new LessonReview
            {
                BookingId = b.Id,
                StudentUserId = b.StudentUserId,
                TutorUserId = b.TutorUserId,
                Rating = i % 5 == 2 ? 4 : i % 7 == 3 ? 3 : 5,
                Comment = comments[i % comments.Length],
                CreatedAt = b.ScheduledAt.AddHours(3),
            });
        }

        db.Payouts.Add(new Payout
        {
            TutorUserId = sarah.Id,
            Amount = 650m,
            Status = PayoutStatus.Paid,
            Reference = "IKP-0001",
            RequestedAt = now.AddDays(-40),
            PaidAt = now.AddDays(-37),
        });

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedConversationsAsync(AppDbContext db, Guid sarahId, Guid alexId, CancellationToken ct)
    {
        if (await db.Conversations.AnyAsync(ct))
        {
            return;
        }

        var maria = await db.Users.FirstOrDefaultAsync(u => u.Email == "maria@example.com", ct);
        var now = DateTime.UtcNow;

        var withAlex = new Conversation { TutorUserId = sarahId, OtherUserId = alexId, CreatedAt = now.AddDays(-3) };
        db.Conversations.Add(withAlex);
        AddMessages(db, withAlex,
            (alexId, "Hi Dr. Chen, can we go over integration by parts again on Friday?", now.AddDays(-3), true),
            (sarahId, "Of course. Bring the two questions you got stuck on and we'll start there.", now.AddDays(-3).AddHours(1), true),
            (alexId, "Thanks! I've uploaded them to my notes.", now.AddDays(-2), true),
            (sarahId, "Great, see you Friday at 16:00.", now.AddHours(-5), false));

        if (maria is not null)
        {
            var withMaria = new Conversation { TutorUserId = sarahId, OtherUserId = maria.Id, CreatedAt = now.AddDays(-1) };
            db.Conversations.Add(withMaria);
            AddMessages(db, withMaria,
                (maria.Id, "Hello, how is Alex doing with calculus? His test is in two weeks.", now.AddHours(-20), false),
                (maria.Id, "Happy to book an extra lesson if it helps.", now.AddHours(-19), false));
        }

        await db.SaveChangesAsync(ct);
    }

    private static void AddMessages(AppDbContext db, Conversation conversation, params (Guid Sender, string Body, DateTime SentAt, bool Read)[] messages)
    {
        foreach (var (sender, body, sentAt, read) in messages)
        {
            db.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                SenderUserId = sender,
                Body = body,
                SentAt = sentAt,
                ReadAt = read ? sentAt.AddMinutes(10) : null,
            });
        }
        var last = messages[^1];
        conversation.LastMessageAt = last.SentAt;
        conversation.LastSenderUserId = last.Sender;
        conversation.LastMessagePreview = last.Body;
    }

    private static async Task BackfillCommissionAsync(AppDbContext db, CancellationToken ct)
    {
        var missing = await db.Bookings.Include(b => b.Subject).Where(b => b.CommissionPercent == 0m).ToListAsync(ct);
        foreach (var booking in missing)
        {
            booking.CommissionPercent = booking.Subject.CommissionPercent;
        }
        if (missing.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task AddProfileAsync(
        AppDbContext db, IFileStorage storage, Guid tutorId, VerificationStatus status, DateTime? submittedAt,
        decimal rate, int years, string? bio, Guid[] subjectIds, DocumentType[] documents, CancellationToken ct)
    {
        db.TutorProfiles.Add(new TutorProfile
        {
            UserId = tutorId,
            HourlyRate = rate,
            YearsExperience = years,
            Bio = bio,
            VerificationStatus = status,
            SubmittedAt = submittedAt,
        });

        foreach (var subjectId in subjectIds)
        {
            db.TutorSubjects.Add(new TutorSubject { TutorUserId = tutorId, SubjectId = subjectId });
        }

        foreach (var type in documents)
        {
            var id = Guid.NewGuid();
            var key = $"tutors/{tutorId}/{id}-seed.pdf";
            var bytes = SamplePdf(type.ToString());
            await using var stream = new MemoryStream(bytes);
            await storage.SaveAsync(key, stream, "application/pdf", ct);
            db.VerificationDocuments.Add(new VerificationDocument
            {
                Id = id,
                TutorUserId = tutorId,
                DocumentType = type,
                FileName = type switch
                {
                    DocumentType.IdDocument => "id-document.pdf",
                    DocumentType.Qualification => "degree-certificate.pdf",
                    _ => "police-clearance.pdf",
                },
                FileUrl = key,
                ContentType = "application/pdf",
                SizeBytes = bytes.Length,
                UploadedAt = (submittedAt ?? DateTime.UtcNow).AddHours(-2),
            });
        }
    }

    private static void AddAvailability(AppDbContext db, Guid tutorId, DayOfWeek[] days, int start, int end)
    {
        foreach (var day in days)
        {
            db.TutorAvailabilitySlots.Add(new TutorAvailabilitySlot
            {
                TutorUserId = tutorId,
                DayOfWeek = day,
                StartMinute = start,
                EndMinute = end,
            });
        }
    }

    private static Booking NewBooking(
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
        CompletedAt = status == BookingStatus.Completed ? scheduledAt.AddMinutes(minutes) : null,
    };

    private static byte[] SamplePdf(string label)
    {
        var text = $"Sample {label} for development";
        var stream = $"BT /F1 18 Tf 72 720 Td ({text}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {stream.Length} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };

        var sb = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            sb.Append($"{offset:D10} 00000 n \n");
        }
        sb.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
