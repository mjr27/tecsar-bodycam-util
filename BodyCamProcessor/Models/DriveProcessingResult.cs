namespace BodyCamProcessor.Models;

public sealed record DriveProcessingResult(
    string DriveRoot,
    string DiskName,
    int MovedFiles,
    long MovedBytes,
    bool SourceFound,
    string? Error);
