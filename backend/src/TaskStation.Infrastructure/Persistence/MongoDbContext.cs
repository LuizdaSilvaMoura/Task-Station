using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using TaskStation.Domain.Entities;
using TaskStation.Domain.Enums;

namespace TaskStation.Infrastructure.Persistence;

public sealed class MongoDbContext
{
    public IMongoDatabase Database { get; }

    public IMongoCollection<TaskItem> Tasks
        => Database.GetCollection<TaskItem>("tasks");

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        RegisterConventions();
        RegisterClassMaps();

        var client = new MongoClient(settings.Value.ConnectionString);
        Database = client.GetDatabase(settings.Value.DatabaseName);

        EnsureIndexes();
    }

    private static void RegisterConventions()
    {
        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreExtraElementsConvention(true)
        };

        ConventionRegistry.Register("TaskStationConventions", pack, _ => true);
    }

    private static void RegisterClassMaps()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(TaskItem)))
            return;

        BsonClassMap.RegisterClassMap<TaskItem>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(c => c.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(c => c.Status)
                .SetSerializer(new EnumSerializer<TaskItemStatus>(BsonType.String));

            cm.SetIgnoreExtraElements(true);
        });
    }

    private void EnsureIndexes()
    {
        var indexKeys = Builders<TaskItem>.IndexKeys
            .Ascending(t => t.Status)
            .Ascending(t => t.DueDate);

        Tasks.Indexes.CreateOne(new CreateIndexModel<TaskItem>(
            indexKeys,
            new CreateIndexOptions { Name = "ix_status_duedate" }));
    }
}
