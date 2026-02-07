using FluentValidation;
using TaskStation.Application.DTOs;
using TaskStation.Application.Interfaces;
using TaskStation.Application.Mappers;
using TaskStation.Domain.Entities;
using TaskStation.Domain.Enums;
using TaskStation.Domain.Exceptions;
using TaskStation.Domain.Repositories;

namespace TaskStation.Application.Services;

public sealed class TaskAppService : ITaskAppService
{
    private readonly ITaskRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IFileStorageSettings _fileStorageSettings;

    public TaskAppService(
        ITaskRepository repository,
        IFileStorageService fileStorage,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IFileStorageSettings fileStorageSettings)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _fileStorageSettings = fileStorageSettings;
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        // 1. Validate the request
        var validationResult = await _createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            throw new TaskValidationException(errors);
        }

        // 2. Create domain entity (invariants enforced in constructor)
        var task = new TaskItem(
            title: request.Title,
            slaHours: request.SlaHours,
            fileUrl: null);

        // 3. Handle file upload based on S3 configuration
        if (request.File is not null)
        {
            if (_fileStorageSettings.IsS3Enabled)
            {
                // Upload to S3
                await using var stream = request.File.OpenReadStream();
                var fileUrl = await _fileStorage.UploadAsync(
                    stream,
                    request.File.FileName,
                    request.File.ContentType,
                    ct);
                task.SetFileUrl(fileUrl);
            }
            else
            {
                // Store directly in MongoDB
                await using var stream = request.File.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, ct);
                task.SetFileData(
                    memoryStream.ToArray(),
                    request.File.FileName,
                    request.File.ContentType);
            }
        }

        // 4. Persist to MongoDB
        await _repository.InsertAsync(task, ct);

        return TaskMapper.ToResponse(task);
    }

    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(string? statusFilter, CancellationToken ct = default)
    {
        IReadOnlyList<TaskItem> tasks;

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            if (string.Equals(statusFilter, "OVERDUE", StringComparison.OrdinalIgnoreCase))
            {
                tasks = await _repository.GetOverdueTasksAsync(ct);
            }
            else if (Enum.TryParse<TaskItemStatus>(statusFilter, ignoreCase: true, out var status))
            {
                tasks = await _repository.GetByStatusAsync(status, ct);
            }
            else
            {
                throw new TaskValidationException($"Invalid status filter: '{statusFilter}'.");
            }
        }
        else
        {
            tasks = await _repository.GetAllAsync(ct);
        }

        return TaskMapper.ToResponseList(tasks);
    }

    public async Task<TaskResponse> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(TaskItem), id);

        return TaskMapper.ToResponse(task);
    }

    public async Task<TaskResponse> UpdateAsync(string id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        // 1. Validate the request
        var validationResult = await _updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            throw new TaskValidationException(errors);
        }

        // 2. Get existing task
        var task = await _repository.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(TaskItem), id);

        // 3. Update title
        task.UpdateTitle(request.Title);

        // 4. Update SLA
        task.UpdateSla(request.SlaHours);

        // 5. Update status
        var normalizedStatus = request.Status.ToUpperInvariant();
        switch (normalizedStatus)
        {
            case "DONE":
                task.MarkAsDone();
                break;
            case "PENDING":
                task.MarkAsPending();
                break;
            default:
                throw new TaskValidationException($"Invalid status: '{request.Status}'.");
        }

        // 6. Handle file operations
        if (request.RemoveFile)
        {
            // Remove existing file
            task.RemoveFile();
        }
        if (request.File is not null)
        {
            // Upload new file if provided
            if (_fileStorageSettings.IsS3Enabled)
            {
                // Upload to S3
                await using var stream = request.File.OpenReadStream();
                var fileUrl = await _fileStorage.UploadAsync(
                    stream,
                    request.File.FileName,
                    request.File.ContentType,
                    ct);
                task.SetFileUrl(fileUrl);
            }
            else
            {
                // Store directly in MongoDB
                await using var stream = request.File.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, ct);
                task.SetFileData(
                    memoryStream.ToArray(),
                    request.File.FileName,
                    request.File.ContentType);
            }
        }

        // 7. Persist changes
        await _repository.UpdateAsync(task, ct);

        return TaskMapper.ToResponse(task);
    }

    public async Task<TaskResponse> UpdateStatusAsync(string id, UpdateTaskStatusRequest request, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(TaskItem), id);

        var normalizedStatus = request.Status?.ToUpperInvariant();

        switch (normalizedStatus)
        {
            case "DONE":
                task.MarkAsDone();
                break;
            case "PENDING":
                // No-op if already pending, or re-validate
                break;
            default:
                throw new TaskValidationException($"Invalid status transition: '{request.Status}'.");
        }

        await _repository.UpdateAsync(task, ct);

        return TaskMapper.ToResponse(task);
    }

    public async Task<(byte[] FileData, string FileName, string ContentType)?> GetFileAsync(string id, CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(TaskItem), id);

        if (task.FileData == null || string.IsNullOrEmpty(task.FileName) || string.IsNullOrEmpty(task.FileContentType))
        {
            return null;
        }

        return (task.FileData, task.FileName, task.FileContentType);
    }
}
