using TaskStation.Application.DTOs;
using TaskStation.Domain.Entities;
using TaskStation.Domain.Enums;

namespace TaskStation.Application.Mappers;

public static class TaskMapper
{
    public static TaskResponse ToResponse(TaskItem entity)
    {
        // Generate file URL based on storage location
        string? fileUrl = null;
        string? fileName = null;
        string? fileContentType = null;
        string? fileDataBase64 = null;

        if (!string.IsNullOrEmpty(entity.FileUrl))
        {
            // File stored in S3
            fileUrl = entity.FileUrl;
            // Extract filename from S3 URL (last part after /)
            fileName = entity.FileUrl.Split('/').LastOrDefault();
        }
        else if (entity.FileData != null && entity.FileData.Length > 0)
        {
            // File stored in MongoDB - include file data as base64
            fileUrl = $"/api/tasks/{entity.Id}/file";
            fileName = entity.FileName;
            fileContentType = entity.FileContentType;
            fileDataBase64 = Convert.ToBase64String(entity.FileData);
        }

        return new TaskResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            SlaHours = entity.SlaHours,
            SlaExpirationDate = entity.DueDate,
            Status = MapStatus(entity.Status),
            FileUrl = fileUrl,
            FileName = fileName,
            FileContentType = fileContentType,
            FileDataBase64 = fileDataBase64
        };
    }

    public static IReadOnlyList<TaskResponse> ToResponseList(IEnumerable<TaskItem> entities)
    {
        return entities.Select(ToResponse).ToList().AsReadOnly();
    }

    private static string MapStatus(TaskItemStatus status) => status switch
    {
        TaskItemStatus.Pending => "PENDING",
        TaskItemStatus.Done => "DONE",
        TaskItemStatus.Overdue => "OVERDUE",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
