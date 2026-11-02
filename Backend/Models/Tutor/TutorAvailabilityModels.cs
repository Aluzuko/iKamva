using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Tutor;

public sealed class AvailabilitySlotDto
{
    [Range(0, 6)]
    public required int DayOfWeek { get; init; }

    [Range(0, 1440)]
    public required int StartMinute { get; init; }

    [Range(0, 1440)]
    public required int EndMinute { get; init; }
}

public sealed class AvailabilityResponse
{
    public required string TimeZone { get; init; }
    public required IReadOnlyList<AvailabilitySlotDto> Slots { get; init; }
}

public sealed class UpdateAvailabilityRequest
{
    [MaxLength(70)]
    public IReadOnlyList<AvailabilitySlotDto> Slots { get; init; } = Array.Empty<AvailabilitySlotDto>();
}
