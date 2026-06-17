using BodyCamProcessor.Models;

namespace BodyCamProcessor.Services;

public sealed class DriveProcessingCoordinator
{
    private readonly FileMoveService _fileMoveService;
    private readonly LogService _logService;
    private readonly DriveEjectService _ejectService;
    private readonly object _sync = new();
    private readonly Dictionary<string, DriveProcessingProgress> _active = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _processedThisConnection = new(StringComparer.OrdinalIgnoreCase);
    private bool _isPaused;
    private AppSettings _settings;

    public DriveProcessingCoordinator(
        AppSettings settings,
        FileMoveService fileMoveService,
        LogService logService,
        DriveEjectService ejectService)
    {
        _settings = settings;
        _fileMoveService = fileMoveService;
        _logService = logService;
        _ejectService = ejectService;
    }

    public event EventHandler? StateChanged;
    public event EventHandler<DriveProcessingResult>? DriveCompleted;

    public IReadOnlyCollection<DriveProcessingProgress> Active
    {
        get
        {
            lock (_sync)
            {
                return _active.Values.ToList();
            }
        }
    }

    public bool IsPaused
    {
        get
        {
            lock (_sync)
            {
                return _isPaused;
            }
        }
    }

    public void SetPaused(bool isPaused)
    {
        lock (_sync)
        {
            if (_isPaused == isPaused)
            {
                return;
            }

            _isPaused = isPaused;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
    }

    public void MarkDisconnected(string driveRoot)
    {
        lock (_sync)
        {
            _processedThisConnection.Remove(driveRoot);
            _active.Remove(driveRoot);
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool TryStart(DriveSnapshot drive)
    {
        lock (_sync)
        {
            if (_isPaused || !_settings.AllowedDiskNames.Contains(drive.VolumeLabel, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (_processedThisConnection.Contains(drive.RootPath) || _active.ContainsKey(drive.RootPath))
            {
                return false;
            }

            _processedThisConnection.Add(drive.RootPath);
            _active[drive.RootPath] = new DriveProcessingProgress(drive.RootPath, drive.VolumeLabel, 0, 0, 0, 0, string.Empty);
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
        _ = Task.Run(() => ProcessAsync(drive));
        return true;
    }

    private async Task ProcessAsync(DriveSnapshot drive)
    {
        DriveProcessingResult result;
        try
        {
            var progress = new Progress<DriveProcessingProgress>(p =>
            {
                lock (_sync)
                {
                    _active[p.DriveRoot] = p;
                }

                StateChanged?.Invoke(this, EventArgs.Empty);
            });

            result = await _fileMoveService.MoveAsync(drive, _settings, progress, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result = new DriveProcessingResult(drive.RootPath, drive.VolumeLabel, 0, 0, SourceFound: true, ex.Message);
        }

        try
        {
            await _logService.UpsertDriveTotalsAsync(_settings, result, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Keep completion path alive even if logging fails.
        }

        await _ejectService.TryEjectAsync(drive.RootPath).ConfigureAwait(false);

        lock (_sync)
        {
            _active.Remove(drive.RootPath);
        }

        DriveCompleted?.Invoke(this, result);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
