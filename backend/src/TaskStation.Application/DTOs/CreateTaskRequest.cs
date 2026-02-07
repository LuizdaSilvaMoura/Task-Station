using Microsoft.AspNetCore.Http;

namespace TaskStation.Application.DTOs;

public sealed record CreateTaskRequest
{
    public string Title { get; init; } = null!;
    public int SlaHours { get; init; }
    public IFormFile? File { get; init; }
}
