namespace TaskStation.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns the public URL.
    /// </summary>
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default);
}
