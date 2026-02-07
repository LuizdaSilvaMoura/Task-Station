using Microsoft.Extensions.Options;
using TaskStation.Application.Interfaces;

namespace TaskStation.Infrastructure.Storage;

public sealed class FileStorageSettings : IFileStorageSettings
{
    private readonly S3Settings _s3Settings;

    public FileStorageSettings(IOptions<S3Settings> s3Settings)
    {
        _s3Settings = s3Settings.Value;
    }

    public bool IsS3Enabled => _s3Settings.Enabled;
}
