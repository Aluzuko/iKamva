using Backend.Domain.Enums;

namespace Backend.Models.Admin;

public sealed class DisputeDto
{
    public required Guid DisputeId { get; init; }
    public required string RaisedByName { get; init; }
    public required string Subject { get; init; }
    public required string Description { get; init; }
    public required DisputeStatus Status { get; init; }
    public string? Resolution { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}
