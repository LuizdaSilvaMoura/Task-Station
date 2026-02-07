using Microsoft.AspNetCore.Http;

namespace TaskStation.Application.DTOs;

public sealed record UpdateTaskRequest
{
    public string Title { get; init; } = null!;
    public int SlaHours { get; init; }
    public string Status { get; init; } = null!;
    public IFormFile? File { get; init; }
    public bool RemoveFile { get; init; }
}
