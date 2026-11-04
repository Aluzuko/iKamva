namespace Backend.Infrastructure.Tutors;

public enum ServiceOutcome
{
    Ok,
    NotFound,
    Invalid,
    Conflict,
}

public record ServiceResult(ServiceOutcome Outcome, string? Message = null)
{
    public static ServiceResult Ok() => new(ServiceOutcome.Ok);
    public static ServiceResult NotFound(string? message = null) => new(ServiceOutcome.NotFound, message);
    public static ServiceResult Invalid(string message) => new(ServiceOutcome.Invalid, message);
    public static ServiceResult Conflict(string message) => new(ServiceOutcome.Conflict, message);
}

public sealed record ServiceResult<T>(ServiceOutcome Outcome, T? Value = default, string? Message = null)
{
    public static ServiceResult<T> Ok(T value) => new(ServiceOutcome.Ok, value);
    public static ServiceResult<T> From(ServiceResult failure) => new(failure.Outcome, default, failure.Message);
}
