using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Tutors;
using Backend.Models.Messages;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Messaging;

public interface IMessagingService
{
    Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(Guid me, CancellationToken ct = default);

    Task<MessagePageResponse?> GetMessagesAsync(Guid me, Guid conversationId, DateTime? before, CancellationToken ct = default);

    Task<ServiceResult<MessageDto>> SendAsync(Guid me, Guid conversationId, string body, CancellationToken ct = default);

    Task<ServiceResult<ConversationDto>> StartAsync(Guid me, Guid recipientId, string body, CancellationToken ct = default);

    Task<bool> MarkReadAsync(Guid me, Guid conversationId, CancellationToken ct = default);

    Task<int> UnreadCountAsync(Guid me, CancellationToken ct = default);

    Task<IReadOnlyList<ContactDto>> GetContactsAsync(Guid me, CancellationToken ct = default);
}

public class MessagingService : IMessagingService
{
    private const int PageSize = 50;

    private readonly AppDbContext _db;

    public MessagingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(Guid me, CancellationToken ct = default)
    {
        var conversations = await _db.Conversations
            .Where(c => (c.TutorUserId == me || c.OtherUserId == me) && c.LastMessageAt != null)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

        var ids = conversations.Select(c => c.Id).ToList();
        var unread = await _db.Messages
            .Where(m => ids.Contains(m.ConversationId) && m.SenderUserId != me && m.ReadAt == null)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count, ct);

        var otherIds = conversations.Select(c => OtherOf(c, me)).Distinct().ToList();
        var others = await _db.Users.Where(u => otherIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        return conversations
            .Where(c => others.ContainsKey(OtherOf(c, me)))
            .Select(c => Map(c, me, others[OtherOf(c, me)], unread.TryGetValue(c.Id, out var n) ? n : 0))
            .ToList();
    }

    public async Task<MessagePageResponse?> GetMessagesAsync(Guid me, Guid conversationId, DateTime? before, CancellationToken ct = default)
    {
        var conversation = await FindForParticipantAsync(me, conversationId, ct);
        if (conversation is null)
        {
            return null;
        }
        var otherId = OtherOf(conversation, me);
        var other = await _db.Users.FirstOrDefaultAsync(u => u.Id == otherId, ct);
        if (other is null)
        {
            return null;
        }

        var query = _db.Messages.Where(m => m.ConversationId == conversationId);
        if (before is not null)
        {
            var cutoff = before.Value.ToUniversalTime();
            query = query.Where(m => m.SentAt < cutoff);
        }

        var page = await query.OrderByDescending(m => m.SentAt).Take(PageSize + 1).ToListAsync(ct);
        var hasMore = page.Count > PageSize;
        var messages = page.Take(PageSize).OrderBy(m => m.SentAt).Select(m => MapMessage(m, me)).ToList();

        var unread = await _db.Messages.CountAsync(m => m.ConversationId == conversationId && m.SenderUserId != me && m.ReadAt == null, ct);

        return new MessagePageResponse { Conversation = Map(conversation, me, other, unread), Messages = messages, HasMore = hasMore };
    }

    public async Task<ServiceResult<MessageDto>> SendAsync(Guid me, Guid conversationId, string body, CancellationToken ct = default)
    {
        var text = Clean(body);
        if (text is null)
        {
            return ServiceResult<MessageDto>.From(ServiceResult.Invalid("Write a message first."));
        }

        var conversation = await FindForParticipantAsync(me, conversationId, ct);
        if (conversation is null)
        {
            return ServiceResult<MessageDto>.From(ServiceResult.NotFound());
        }

        var otherId = OtherOf(conversation, me);
        var otherActive = await _db.Users.AnyAsync(u => u.Id == otherId && u.IsActive, ct);
        if (!otherActive)
        {
            return ServiceResult<MessageDto>.From(ServiceResult.Conflict("This person’s account is no longer active."));
        }

        var message = Append(conversation, me, text);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<MessageDto>.Ok(MapMessage(message, me));
    }

    public async Task<ServiceResult<ConversationDto>> StartAsync(Guid me, Guid recipientId, string body, CancellationToken ct = default)
    {
        var text = Clean(body);
        if (text is null)
        {
            return ServiceResult<ConversationDto>.From(ServiceResult.Invalid("Write a message first."));
        }

        var users = await _db.Users.Where(u => u.Id == me || u.Id == recipientId).ToDictionaryAsync(u => u.Id, ct);
        if (recipientId == me || !users.TryGetValue(me, out var sender) || !users.TryGetValue(recipientId, out var recipient) || !recipient.IsActive)
        {
            return ServiceResult<ConversationDto>.From(ServiceResult.NotFound("You can’t message this person."));
        }

        var (tutorId, otherId) = sender.Role == UserRole.Tutor ? (sender.Id, recipient.Id) : (recipient.Id, sender.Id);
        var conversation = await _db.Conversations.FirstOrDefaultAsync(c => c.TutorUserId == tutorId && c.OtherUserId == otherId, ct);

        if (conversation is null)
        {
            if (!await CanContactAsync(sender, recipient, ct))
            {
                return ServiceResult<ConversationDto>.From(ServiceResult.Conflict("You can only message tutors you can book, or people connected to your lessons."));
            }
            conversation = new Conversation { TutorUserId = tutorId, OtherUserId = otherId };
            _db.Conversations.Add(conversation);
        }

        Append(conversation, me, text);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult<ConversationDto>.From(ServiceResult.Conflict("Something changed while sending. Try again."));
        }

        return ServiceResult<ConversationDto>.Ok(Map(conversation, me, recipient, 0));
    }

    public async Task<bool> MarkReadAsync(Guid me, Guid conversationId, CancellationToken ct = default)
    {
        var conversation = await FindForParticipantAsync(me, conversationId, ct);
        if (conversation is null)
        {
            return false;
        }

        var unread = await _db.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderUserId != me && m.ReadAt == null)
            .ToListAsync(ct);
        if (unread.Count > 0)
        {
            var now = DateTime.UtcNow;
            unread.ForEach(m => m.ReadAt = now);
            await _db.SaveChangesAsync(ct);
        }
        return true;
    }

    public async Task<int> UnreadCountAsync(Guid me, CancellationToken ct = default) =>
        await (
            from m in _db.Messages
            join c in _db.Conversations on m.ConversationId equals c.Id
            where (c.TutorUserId == me || c.OtherUserId == me) && m.SenderUserId != me && m.ReadAt == null
            select m.Id
        ).CountAsync(ct);

    public async Task<IReadOnlyList<ContactDto>> GetContactsAsync(Guid me, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == me, ct);
        if (user is null)
        {
            return Array.Empty<ContactDto>();
        }

        var contacts = new List<ContactDto>();
        switch (user.Role)
        {
            case UserRole.Student:
                var tutors = await (
                    from b in _db.Bookings
                    join t in _db.Users on b.TutorUserId equals t.Id
                    where b.StudentUserId == me && t.IsActive
                    select new { t.Id, t.FullName, Subject = b.Subject.Name }
                ).Distinct().ToListAsync(ct);
                contacts.AddRange(tutors.GroupBy(t => t.Id).Select(g => new ContactDto
                {
                    UserId = g.Key, FullName = g.First().FullName, Role = UserRole.Tutor,
                    Context = "Your " + string.Join(", ", g.Select(x => x.Subject).Distinct()) + " tutor",
                }));
                break;

            case UserRole.Parent:
                var childIds = await ChildIdsAsync(me, ct);
                var childTutors = await (
                    from b in _db.Bookings
                    join t in _db.Users on b.TutorUserId equals t.Id
                    join s in _db.Users on b.StudentUserId equals s.Id
                    where childIds.Contains(b.StudentUserId) && t.IsActive
                    select new { t.Id, t.FullName, Child = s.FullName }
                ).Distinct().ToListAsync(ct);
                contacts.AddRange(childTutors.GroupBy(t => t.Id).Select(g => new ContactDto
                {
                    UserId = g.Key, FullName = g.First().FullName, Role = UserRole.Tutor,
                    Context = string.Join(" and ", g.Select(x => FirstName(x.Child)).Distinct()) + "’s tutor",
                }));
                break;

            case UserRole.Tutor:
                var students = await (
                    from b in _db.Bookings
                    join s in _db.Users on b.StudentUserId equals s.Id
                    where b.TutorUserId == me && s.IsActive
                    select new { s.Id, s.FullName }
                ).Distinct().ToListAsync(ct);
                contacts.AddRange(students.Select(s => new ContactDto { UserId = s.Id, FullName = s.FullName, Role = UserRole.Student, Context = "Your student" }));

                var studentIds = students.Select(s => s.Id).ToList();
                var parents = await (
                    from l in _db.ParentStudentLinks
                    join p in _db.Users on l.ParentUserId equals p.Id
                    join s in _db.Users on l.StudentUserId equals s.Id
                    where l.StudentUserId != null && studentIds.Contains(l.StudentUserId.Value) && l.Status == LinkStatus.Active && p.IsActive
                    select new { p.Id, p.FullName, Child = s.FullName }
                ).ToListAsync(ct);
                contacts.AddRange(parents.GroupBy(p => p.Id).Select(g => new ContactDto
                {
                    UserId = g.Key, FullName = g.First().FullName, Role = UserRole.Parent,
                    Context = string.Join(" and ", g.Select(x => FirstName(x.Child)).Distinct()) + "’s parent",
                }));
                break;
        }

        return contacts.OrderBy(c => c.FullName).ToList();
    }

    private async Task<bool> CanContactAsync(User sender, User recipient, CancellationToken ct)
    {
        switch (sender.Role, recipient.Role)
        {
            case (UserRole.Student, UserRole.Tutor):
                return await _db.TutorProfiles.AnyAsync(t => t.UserId == recipient.Id && t.VerificationStatus == VerificationStatus.Approved, ct)
                    || await _db.Bookings.AnyAsync(b => b.StudentUserId == sender.Id && b.TutorUserId == recipient.Id, ct);

            case (UserRole.Parent, UserRole.Tutor):
                var childIds = await ChildIdsAsync(sender.Id, ct);
                return await _db.Bookings.AnyAsync(b => b.TutorUserId == recipient.Id && childIds.Contains(b.StudentUserId), ct);

            case (UserRole.Tutor, UserRole.Student):
                return await _db.Bookings.AnyAsync(b => b.TutorUserId == sender.Id && b.StudentUserId == recipient.Id, ct);

            case (UserRole.Tutor, UserRole.Parent):
                var linkedIds = await ChildIdsAsync(recipient.Id, ct);
                return await _db.Bookings.AnyAsync(b => b.TutorUserId == sender.Id && linkedIds.Contains(b.StudentUserId), ct);

            default:
                return false;
        }
    }

    private async Task<List<Guid>> ChildIdsAsync(Guid parentId, CancellationToken ct) =>
        await _db.ParentStudentLinks
            .Where(l => l.ParentUserId == parentId && l.Status == LinkStatus.Active && l.StudentUserId != null)
            .Select(l => l.StudentUserId!.Value)
            .ToListAsync(ct);

    private async Task<Conversation?> FindForParticipantAsync(Guid me, Guid conversationId, CancellationToken ct) =>
        await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId && (c.TutorUserId == me || c.OtherUserId == me), ct);

    private Message Append(Conversation conversation, Guid sender, string text)
    {
        var message = new Message { ConversationId = conversation.Id, SenderUserId = sender, Body = text };
        _db.Messages.Add(message);
        conversation.LastMessageAt = message.SentAt;
        conversation.LastSenderUserId = sender;
        conversation.LastMessagePreview = text.Length <= 200 ? text : text[..197] + "…";
        return message;
    }

    private static string? Clean(string body)
    {
        var text = body?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }
        return text.Length > 2000 ? text[..2000] : text;
    }

    private static Guid OtherOf(Conversation c, Guid me) => c.TutorUserId == me ? c.OtherUserId : c.TutorUserId;

    private static string FirstName(string fullName) => fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;

    private static ConversationDto Map(Conversation c, Guid me, User other, int unread) => new()
    {
        ConversationId = c.Id,
        OtherUserId = other.Id,
        OtherName = other.FullName,
        OtherRole = other.Role,
        LastMessage = c.LastMessagePreview,
        LastMessageAt = c.LastMessageAt,
        LastMessageMine = c.LastSenderUserId == me,
        UnreadCount = unread,
    };

    private static MessageDto MapMessage(Message m, Guid me) => new()
    {
        MessageId = m.Id,
        Body = m.Body,
        SentAt = m.SentAt,
        Mine = m.SenderUserId == me,
        ReadAt = m.ReadAt,
    };
}
