using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskStation.Infrastructure.Persistence;
using Testcontainers.MongoDb;

namespace TaskStation.IntegrationTests.Fixtures;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7")
        .WithName($"test-mongo-{Guid.NewGuid()}")
        .Build();

    public string TestConnectionString => _mongoContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        // Start MongoDB container
        await _mongoContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing MongoDB configuration
            services.RemoveAll(typeof(MongoDbContext));

            // Configure test MongoDB
            services.Configure<MongoDbSettings>(options =>
            {
                options.ConnectionString = TestConnectionString;
                options.DatabaseName = $"TestDb_{Guid.NewGuid():N}";
            });

            // Re-add MongoDbContext with test configuration
            services.AddSingleton<MongoDbContext>();

            // Disable S3 for tests (use MongoDB for file storage)
            services.PostConfigure<TaskStation.Infrastructure.Storage.S3Settings>(options =>
            {
                options.Enabled = false;
            });
        });

        builder.UseEnvironment("Testing");
    }

    public new async Task DisposeAsync()
    {
        await _mongoContainer.StopAsync();
        await _mongoContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
