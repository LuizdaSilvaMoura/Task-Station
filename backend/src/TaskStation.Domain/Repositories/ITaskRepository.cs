using TaskStation.Domain.Entities;
using TaskStation.Domain.Enums;

namespace TaskStation.Domain.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetByStatusAsync(TaskItemStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetOverdueTasksAsync(CancellationToken ct = default);
    Task InsertAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateManyToOverdueAsync(CancellationToken ct = default);
}
