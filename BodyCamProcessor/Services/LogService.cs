using BodyCamProcessor.Localization;
using BodyCamProcessor.Models;

namespace BodyCamProcessor.Services;

public sealed class LogService
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task UpsertDriveTotalsAsync(AppSettings settings, DriveProcessingResult result, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.DestinationPath) || result.MovedFiles == 0)
        {
            return;
        }

        var dayFolder = GetDayFolder(settings.DestinationPath, DateTime.Now);
        var driveDestinationFolder = Path.Combine(dayFolder, SanitizePathSegment(result.DiskName));
        if (!Directory.Exists(driveDestinationFolder))
        {
            return;
        }

        var totals = GetFolderTotals(driveDestinationFolder);
        if (totals.FileCount == 0)
        {
            return;
        }

        Directory.CreateDirectory(dayFolder);

        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm} | {result.DiskName} | {totals.FileCount} {Localizer.Get(AppLanguage.English, UiString.Files)} | {FormatBytes(totals.TotalBytes)}";
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            line += $" | {Localizer.Get(AppLanguage.English, UiString.Error)}: {result.Error}";
        }

        var logPath = Path.Combine(dayFolder, "log.txt");

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var lines = File.Exists(logPath)
                ? await File.ReadAllLinesAsync(logPath, cancellationToken).ConfigureAwait(false)
                : [];

            var updatedLines = lines
                .Where(existingLine => !IsLogLineForDisk(existingLine, result.DiskName))
                .Append(line);

            await File.WriteAllLinesAsync(logPath, updatedLines, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public string ReadLog(AppSettings settings, DateTime date)
    {
        var path = Path.Combine(GetDayFolder(settings.DestinationPath, date), "log.txt");
        return File.Exists(path) ? File.ReadAllText(path) : Localizer.Get(settings.Language, UiString.NoLogForSelectedDate);
    }

    public static string GetDayFolder(string destinationPath, DateTime date) =>
        Path.Combine(destinationPath, date.ToString("yyyy"), date.ToString("MM"), date.ToString("yyyy-MM-dd"));

    public static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} B" : $"{size:0.0} {units[unit]}";
    }

    private static (int FileCount, long TotalBytes) GetFolderTotals(string folder)
    {
        var fileCount = 0;
        var totalBytes = 0L;

        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            try
            {
                fileCount++;
                totalBytes += new FileInfo(file).Length;
            }
            catch
            {
                // Ignore files that disappear or become inaccessible while totals are calculated.
            }
        }

        return (fileCount, totalBytes);
    }

    private static bool IsLogLineForDisk(string line, string diskName)
    {
        var parts = line.Split('|', 4, StringSplitOptions.TrimEntries);
        return parts.Length >= 2 && string.Equals(parts[1], diskName, StringComparison.OrdinalIgnoreCase);
    }
}
