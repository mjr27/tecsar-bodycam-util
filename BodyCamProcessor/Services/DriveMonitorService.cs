using BodyCamProcessor.Models;

namespace BodyCamProcessor.Services;

public sealed class DriveMonitorService : IDisposable
{
    private readonly DriveDiscoveryService _discoveryService;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(3));
    private readonly CancellationTokenSource _cts = new();
    private readonly HashSet<string> _knownDriveRoots = new(StringComparer.OrdinalIgnoreCase);
    private Task? _loopTask;

    public DriveMonitorService(DriveDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    public event EventHandler<DriveSnapshot>? DriveConnected;
    public event EventHandler<string>? DriveRemoved;

    public void Start()
    {
        if (_loopTask is not null)
        {
            return;
        }

        foreach (var drive in _discoveryService.GetCandidateDrives())
        {
            _knownDriveRoots.Add(drive.RootPath);
        }

        _loopTask = Task.Run(MonitorAsync);
    }

    private async Task MonitorAsync()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
            {
                var current = _discoveryService.GetCandidateDrives();
                var currentRoots = current.Select(d => d.RootPath).ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var drive in current)
                {
                    if (_knownDriveRoots.Add(drive.RootPath))
                    {
                        DriveConnected?.Invoke(this, drive);
                    }
                }

                foreach (var removedRoot in _knownDriveRoots.Except(currentRoots, StringComparer.OrdinalIgnoreCase).ToList())
                {
                    _knownDriveRoots.Remove(removedRoot);
                    DriveRemoved?.Invoke(this, removedRoot);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _timer.Dispose();
        _cts.Dispose();
    }
}
