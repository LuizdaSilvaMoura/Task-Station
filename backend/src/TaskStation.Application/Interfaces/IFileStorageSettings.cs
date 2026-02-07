namespace TaskStation.Application.Interfaces;

public interface IFileStorageSettings
{
    bool IsS3Enabled { get; }
}
