namespace BodyCamProcessor.Models;

public sealed record DriveProcessingProgress(string DriveRoot, string DiskName, int MovedFiles, long MovedBytes, string CurrentFile);
