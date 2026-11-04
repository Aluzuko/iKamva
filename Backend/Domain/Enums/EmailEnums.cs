namespace Backend.Domain.Enums;

public enum EmailCategory
{
    General = 0,
    Welcome = 1,
    AccountVerification = 2,
    PasswordReset = 3,
    StudentInvite = 4,
    Invoice = 5,
    PaymentConfirmation = 6,
    Letter = 7,
    TutorVerification = 8,
}

public enum EmailStatus
{
    Pending = 0,
    Sending = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4,
}

public enum EmailPriority
{
    High = 0,
    Normal = 1,
    Low = 2,
}
