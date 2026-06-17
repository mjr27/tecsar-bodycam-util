using BodyCamProcessor.Models;

namespace BodyCamProcessor.Services;

public sealed class FileMoveService
{
    public async Task<DriveProcessingResult> MoveAsync(
        DriveSnapshot drive,
        AppSettings settings,
        IProgress<DriveProcessingProgress>? progress,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var sourcePath = BuildSourcePath(drive.RootPath, settings.SourcePath);
            if (!Directory.Exists(sourcePath))
            {
                return new DriveProcessingResult(drive.RootPath, drive.VolumeLabel, 0, 0, SourceFound: false, Error: null);
            }

            var destinationRoot = Path.Combine(
                LogService.GetDayFolder(settings.DestinationPath, DateTime.Now),
                LogService.SanitizePathSegment(drive.VolumeLabel));

            var movedFiles = 0;
            var movedBytes = 0L;
            var sourceBytes = GetFolderSize(sourcePath);
            var destinationBytes = GetFolderSize(destinationRoot);

            progress?.Report(new DriveProcessingProgress(
                drive.RootPath,
                drive.VolumeLabel,
                movedFiles,
                movedBytes,
                destinationBytes,
                sourceBytes + destinationBytes,
                string.Empty));

            foreach (var sourceFile in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                var destinationFile = Path.Combine(destinationRoot, relativePath);
                if (File.Exists(destinationFile))
                {
                    continue;
                }

                var destinationDirectory = Path.GetDirectoryName(destinationFile);
                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                var fileSize = new FileInfo(sourceFile).Length;
                if (TryMoveWithRetry(sourceFile, destinationFile, cancellationToken))
                {
                    movedFiles++;
                    movedBytes += fileSize;
                    sourceBytes = Math.Max(0, sourceBytes - fileSize);
                    destinationBytes += fileSize;
                    progress?.Report(new DriveProcessingProgress(
                        drive.RootPath,
                        drive.VolumeLabel,
                        movedFiles,
                        movedBytes,
                        destinationBytes,
                        sourceBytes + destinationBytes,
                        relativePath));
                }
            }

            RemoveEmptyDirectories(sourcePath);
            return new DriveProcessingResult(drive.RootPath, drive.VolumeLabel, movedFiles, movedBytes, SourceFound: true, Error: null);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildSourcePath(string driveRoot, string configuredPath)
    {
        var trimmed = configuredPath.Trim().TrimStart('\\', '/');
        return Path.Combine(driveRoot, trimmed);
    }

    private static bool TryMoveWithRetry(string sourceFile, string destinationFile, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                File.Move(sourceFile, destinationFile);
                return true;
            }
            catch (IOException) when (attempt < 3)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500 * attempt));
            }
            catch (UnauthorizedAccessException) when (attempt < 3)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500 * attempt));
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        return false;
    }

    private static long GetFolderSize(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return 0;
        }

        var totalBytes = 0L;
        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            try
            {
                totalBytes += new FileInfo(file).Length;
            }
            catch
            {
                // Ignore files that disappear or cannot be read while progress is calculated.
            }
        }

        return totalBytes;
    }

    private static void RemoveEmptyDirectories(string root)
    {
        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                     .OrderByDescending(d => d.Length))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }

}
