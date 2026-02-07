using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskStation.Application.Interfaces;

namespace TaskStation.Infrastructure.Storage;

public sealed class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _settings;
    private readonly ILogger<S3FileStorageService> _logger;

    public S3FileStorageService(
        IAmazonS3 s3Client,
        IOptions<S3Settings> settings,
        ILogger<S3FileStorageService> logger)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);

        var key = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        using var transferUtility = new TransferUtility(_s3Client);
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = key,
            BucketName = _settings.BucketName,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await transferUtility.UploadAsync(uploadRequest, ct);

        var fileUrl = $"{_settings.ServiceUrl}/{_settings.BucketName}/{key}";

        _logger.LogInformation("File uploaded to S3: {FileUrl}", fileUrl);

        return fileUrl;
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        try
        {
            await _s3Client.EnsureBucketExistsAsync(_settings.BucketName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure S3 bucket exists. It may already exist.");
        }
    }
}
