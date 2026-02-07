namespace TaskStation.Infrastructure.Storage;

public sealed class S3Settings
{
    public const string SectionName = "S3";

    public bool Enabled { get; set; } = true;
    public string BucketName { get; set; } = "task-station-files";
    public string ServiceUrl { get; set; } = "http://localhost:4566";
    public string Region { get; set; } = "us-east-1";
    public bool ForcePathStyle { get; set; } = true;
}
