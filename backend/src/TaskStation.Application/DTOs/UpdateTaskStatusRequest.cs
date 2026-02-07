namespace TaskStation.Application.DTOs;

public sealed record UpdateTaskStatusRequest
{
    public string Status { get; init; } = null!;
}
