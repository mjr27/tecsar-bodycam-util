namespace BodyCamProcessor.Models;

public sealed record DriveProcessingProgress(
    string DriveRoot,
    string DiskName,
    int MovedFiles,
    long MovedBytes,
    long DestinationBytes,
    long TotalBytes,
    string CurrentFile);
