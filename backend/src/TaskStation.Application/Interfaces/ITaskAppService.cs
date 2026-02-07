using TaskStation.Application.DTOs;

namespace TaskStation.Application.Interfaces;

public interface ITaskAppService
{
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(string? statusFilter, CancellationToken ct = default);
    Task<TaskResponse> GetByIdAsync(string id, CancellationToken ct = default);
    Task<TaskResponse> UpdateAsync(string id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> UpdateStatusAsync(string id, UpdateTaskStatusRequest request, CancellationToken ct = default);
    Task<(byte[] FileData, string FileName, string ContentType)?> GetFileAsync(string id, CancellationToken ct = default);
}
