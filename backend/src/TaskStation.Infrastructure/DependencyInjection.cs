using Amazon.S3;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskStation.Application.Interfaces;
using TaskStation.Application.Services;
using TaskStation.Domain.Repositories;
using TaskStation.Infrastructure.Persistence;
using TaskStation.Infrastructure.Persistence.Repositories;
using TaskStation.Infrastructure.Storage;

namespace TaskStation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── MongoDB ──
        services.Configure<MongoDbSettings>(
            configuration.GetSection(MongoDbSettings.SectionName));
        services.AddSingleton<MongoDbContext>();
        services.AddScoped<ITaskRepository, TaskRepository>();

        // ── AWS S3 (LocalStack) ──
        var s3Settings = configuration.GetSection(S3Settings.SectionName).Get<S3Settings>()
            ?? new S3Settings();
        services.Configure<S3Settings>(configuration.GetSection(S3Settings.SectionName));

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = s3Settings.ServiceUrl,
                ForcePathStyle = s3Settings.ForcePathStyle
            };
            return new AmazonS3Client("test", "test", config);
        });
        services.AddScoped<IFileStorageService, S3FileStorageService>();
        services.AddSingleton<IFileStorageSettings, FileStorageSettings>();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ── Application Services ──
        services.AddScoped<ITaskAppService, TaskAppService>();

        // ── FluentValidation — register all validators from Application assembly ──
        services.AddValidatorsFromAssemblyContaining<ITaskAppService>();

        return services;
    }
}
