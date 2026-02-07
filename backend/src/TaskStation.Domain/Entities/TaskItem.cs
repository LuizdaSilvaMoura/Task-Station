using TaskStation.Domain.Enums;
using TaskStation.Domain.Exceptions;

namespace TaskStation.Domain.Entities;

/// <summary>
/// Rich domain entity representing a Task with SLA tracking.
/// All invariants are enforced through the constructor and behavior methods.
/// </summary>
public sealed class TaskItem
{
    public string Id { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int SlaHours { get; private set; }
    public DateTime DueDate { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public string? FileUrl { get; private set; }

    // File storage for MongoDB (when S3 is disabled)
    public byte[]? FileData { get; private set; }
    public string? FileName { get; private set; }
    public string? FileContentType { get; private set; }

    // Required by MongoDB driver for deserialization
    private TaskItem() { }

    public TaskItem(string title, int slaHours, string? description = null, string? fileUrl = null)
    {
        GuardAgainstInvalidTitle(title);
        GuardAgainstInvalidSla(slaHours);

        Title = title.Trim();
        Description = description?.Trim();
        SlaHours = slaHours;
        CreatedAt = DateTime.UtcNow;
        DueDate = CreatedAt.AddHours(slaHours);
        Status = TaskItemStatus.Pending;
        FileUrl = fileUrl;
    }

    public void MarkAsDone()
    {
        if (Status == TaskItemStatus.Done)
            throw new DomainException("Task is already completed.");

        Status = TaskItemStatus.Done;
    }

    public void MarkAsOverdue()
    {
        if (Status == TaskItemStatus.Done)
            return; // Completed tasks cannot become overdue

        if (DateTime.UtcNow < DueDate)
            throw new DomainException("Cannot mark a task as overdue before its due date.");

        Status = TaskItemStatus.Overdue;
    }

    public void SetFileUrl(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new DomainException("File URL cannot be empty.");

        FileUrl = fileUrl;
    }

    public void SetFileData(byte[] fileData, string fileName, string contentType)
    {
        if (fileData == null || fileData.Length == 0)
            throw new DomainException("File data cannot be empty.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("File name cannot be empty.");

        if (string.IsNullOrWhiteSpace(contentType))
            throw new DomainException("Content type cannot be empty.");

        FileData = fileData;
        FileName = fileName;
        FileContentType = contentType;
        FileUrl = null; // Clear S3 URL when storing in MongoDB
    }

    public void RemoveFile()
    {
        FileUrl = null;
        FileData = null;
        FileName = null;
        FileContentType = null;
    }

    public void UpdateTitle(string newTitle)
    {
        GuardAgainstInvalidTitle(newTitle);
        Title = newTitle.Trim();
    }

    public void UpdateSla(int newSlaHours)
    {
        GuardAgainstInvalidSla(newSlaHours);
        SlaHours = newSlaHours;
        DueDate = CreatedAt.AddHours(newSlaHours);
    }

    public void MarkAsPending()
    {
        if (Status == TaskItemStatus.Done)
            throw new DomainException("Cannot mark a completed task as pending.");

        Status = TaskItemStatus.Pending;
    }

    public bool IsSlaExpired() => Status != TaskItemStatus.Done && DateTime.UtcNow > DueDate;

    private static void GuardAgainstInvalidTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new TaskValidationException("Task title is required.");

        if (title.Trim().Length > 200)
            throw new TaskValidationException("Task title must not exceed 200 characters.");
    }

    private static void GuardAgainstInvalidSla(int slaHours)
    {
        if (slaHours <= 0)
            throw new TaskValidationException("SLA hours must be greater than zero.");

        if (slaHours > 8760) // 1 year
            throw new TaskValidationException("SLA hours must not exceed 8760 (1 year).");
    }
}
