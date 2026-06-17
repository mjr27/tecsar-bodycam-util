using System.Diagnostics;
using System.Media;
using BodyCamProcessor.Models;
using BodyCamProcessor.Services;

namespace BodyCamProcessor;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsService _settingsService = new();
    private readonly DriveDiscoveryService _discoveryService = new();
    private readonly LogService _logService = new();
    private readonly DriveMonitorService _monitorService;
    private readonly DriveProcessingCoordinator _coordinator;
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _appIcon = LoadApplicationIcon();
    private readonly Icon _idleIcon = IconFactory.CreateIdleIcon();
    private readonly Icon _copyingIcon = IconFactory.CreateCopyingIcon();
    private readonly SynchronizationContext _uiContext;
    private ToolStripMenuItem? _pauseResumeMenuItem;
    private LogViewerForm? _logViewerForm;
    private AppSettings _settings;
    private string _lastCompleted = "Idle";

    public TrayApplicationContext()
    {
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _settings = _settingsService.Load();
        _monitorService = new DriveMonitorService(_discoveryService);
        _coordinator = new DriveProcessingCoordinator(_settings, new FileMoveService(), _logService, new DriveEjectService());

        _notifyIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = "Idle",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
        _notifyIcon.MouseUp += NotifyIconOnMouseUp;

        _monitorService.DriveConnected += (_, drive) => _coordinator.TryStart(drive);
        _monitorService.DriveRemoved += (_, driveRoot) => _coordinator.MarkDisconnected(driveRoot);
        _coordinator.StateChanged += (_, _) => Ui(UpdateTrayStatus);
        _coordinator.DriveCompleted += (_, result) => Ui(() => ShowCompleted(result));

        _monitorService.Start();
        UpdateTrayStatus();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        _pauseResumeMenuItem = new ToolStripMenuItem("Pause", null, (_, _) => TogglePaused());
        menu.Items.Add(_pauseResumeMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open Configuration", null, (_, _) => OpenConfiguration());
        menu.Items.Add("Open Destination Folder", null, (_, _) => OpenDestinationFolder());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private void TogglePaused()
    {
        var pause = !_coordinator.IsPaused;
        _coordinator.SetPaused(pause);

        if (!pause)
        {
            ProcessConnectedCandidateDrives();
        }
    }

    private void NotifyIconOnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ToggleLogViewer();
        }
    }

    private void ToggleLogViewer()
    {
        if (_logViewerForm is { IsDisposed: false, Visible: true })
        {
            _logViewerForm.Hide();
            return;
        }

        if (_logViewerForm is null || _logViewerForm.IsDisposed)
        {
            _logViewerForm = new LogViewerForm(_settings, _logService, _coordinator) { Icon = _appIcon };
            _logViewerForm.FormClosed += (_, _) => _logViewerForm = null;
        }

        _logViewerForm.Show();
        _logViewerForm.WindowState = FormWindowState.Normal;
        _logViewerForm.Activate();
    }

    private void OpenConfiguration()
    {
        using var form = new SettingsForm(_settings, _discoveryService) { Icon = _appIcon };
        if (form.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _settings = form.Settings;
        _settingsService.Save(_settings);
        _coordinator.UpdateSettings(_settings);

        ProcessConnectedCandidateDrives();
    }

    private void ProcessConnectedCandidateDrives()
    {
        foreach (var drive in _discoveryService.GetCandidateDrives())
        {
            _coordinator.TryStart(drive);
        }
    }

    private void OpenDestinationFolder()
    {
        Directory.CreateDirectory(_settings.DestinationPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = _settings.DestinationPath,
            UseShellExecute = true
        });
    }

    private void ShowCompleted(DriveProcessingResult result)
    {
        _lastCompleted = $"Completed {result.DiskName}";
        UpdateTrayStatus();

        if (result.MovedFiles == 0)
        {
            return;
        }

        var body = result.SourceFound
            ? $"{result.MovedFiles} files moved ({LogService.FormatBytes(result.MovedBytes)})."
            : "Source folder was not found; no files moved.";

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            body = result.Error;
        }

        _notifyIcon.ShowBalloonTip(5000, $"BodyCamProcessor: {result.DiskName}", body, ToolTipIcon.Info);
        SystemSounds.Asterisk.Play();
    }

    private void UpdateTrayStatus()
    {
        if (_pauseResumeMenuItem is not null)
        {
            _pauseResumeMenuItem.Text = _coordinator.IsPaused ? "Resume" : "Pause";
        }

        if (_coordinator.IsPaused)
        {
            _notifyIcon.Icon = _copyingIcon;
            _notifyIcon.Text = TrimTooltip("Paused");
            return;
        }

        var active = _coordinator.Active;
        if (active.Count == 0)
        {
            _notifyIcon.Icon = _idleIcon;
            _notifyIcon.Text = TrimTooltip(_lastCompleted);
            return;
        }

        _notifyIcon.Icon = _copyingIcon;
        _notifyIcon.Text = active.Count == 1
            ? TrimTooltip($"Processing {active.First().DiskName}... {active.First().MovedFiles} files")
            : TrimTooltip($"Processing {active.Count} drives...");
    }

    private static string TrimTooltip(string value) => value.Length <= 63 ? value : value[..63];

    private void Ui(Action action)
    {
        _uiContext.Post(_ => action(), null);
    }

    protected override void ExitThreadCore()
    {
        _logViewerForm?.Close();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _monitorService.Dispose();
        _appIcon.Dispose();
        _idleIcon.Dispose();
        _copyingIcon.Dispose();
        base.ExitThreadCore();
    }

    private static Icon LoadApplicationIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "bodycam.ico");
        return File.Exists(path) ? new Icon(path) : IconFactory.CreateIdleIcon();
    }
}
