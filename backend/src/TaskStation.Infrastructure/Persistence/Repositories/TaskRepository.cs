using MongoDB.Driver;
using TaskStation.Domain.Entities;
using TaskStation.Domain.Enums;
using TaskStation.Domain.Repositories;

namespace TaskStation.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly IMongoCollection<TaskItem> _collection;

    public TaskRepository(MongoDbContext context)
    {
        _collection = context.Tasks;
    }

    public async Task<TaskItem?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<TaskItem>.Filter.Eq(t => t.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await _collection
            .Find(Builders<TaskItem>.Filter.Empty)
            .SortByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByStatusAsync(TaskItemStatus status, CancellationToken ct = default)
    {
        var filter = Builders<TaskItem>.Filter.Eq(t => t.Status, status);
        return await _collection
            .Find(filter)
            .SortByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TaskItem>> GetOverdueTasksAsync(CancellationToken ct = default)
    {
        // Tasks that are Pending AND past their due date, or already marked Overdue
        var filter = Builders<TaskItem>.Filter.Or(
            Builders<TaskItem>.Filter.Eq(t => t.Status, TaskItemStatus.Overdue),
            Builders<TaskItem>.Filter.And(
                Builders<TaskItem>.Filter.Eq(t => t.Status, TaskItemStatus.Pending),
                Builders<TaskItem>.Filter.Lt(t => t.DueDate, DateTime.UtcNow)
            )
        );

        return await _collection
            .Find(filter)
            .SortByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task InsertAsync(TaskItem task, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(task, cancellationToken: ct);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        var filter = Builders<TaskItem>.Filter.Eq(t => t.Id, task.Id);
        await _collection.ReplaceOneAsync(filter, task, cancellationToken: ct);
    }

    public async Task UpdateManyToOverdueAsync(CancellationToken ct = default)
    {
        var filter = Builders<TaskItem>.Filter.And(
            Builders<TaskItem>.Filter.Eq(t => t.Status, TaskItemStatus.Pending),
            Builders<TaskItem>.Filter.Lt(t => t.DueDate, DateTime.UtcNow)
        );

        var update = Builders<TaskItem>.Update
            .Set(t => t.Status, TaskItemStatus.Overdue);

        await _collection.UpdateManyAsync(filter, update, cancellationToken: ct);
    }
}
