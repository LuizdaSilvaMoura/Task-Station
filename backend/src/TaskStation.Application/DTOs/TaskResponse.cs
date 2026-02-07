namespace TaskStation.Application.DTOs;

/// <summary>
/// API response DTO — field names match the frontend contract exactly.
/// </summary>
public sealed record TaskResponse
{
    public string Id { get; init; } = null!;
    public string Title { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public int SlaHours { get; init; }
    public DateTime SlaExpirationDate { get; init; }
    public string Status { get; init; } = null!;
    public string? FileUrl { get; init; }
    public string? FileName { get; init; }
    public string? FileContentType { get; init; }
    public string? FileDataBase64 { get; init; }
}
