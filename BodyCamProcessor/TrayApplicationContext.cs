using System.Diagnostics;
using System.Media;
using BodyCamProcessor.Localization;
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
    private readonly Icon _pausedIcon = IconFactory.CreatePausedIcon();
    private readonly SynchronizationContext _uiContext;
    private ToolStripMenuItem? _pauseResumeMenuItem;
    private ToolStripMenuItem? _openConfigurationMenuItem;
    private ToolStripMenuItem? _openDestinationFolderMenuItem;
    private ToolStripMenuItem? _languageMenuItem;
    private ToolStripMenuItem? _englishMenuItem;
    private ToolStripMenuItem? _ukrainianMenuItem;
    private ToolStripMenuItem? _exitMenuItem;
    private LogViewerForm? _logViewerForm;
    private AppSettings _settings;
    private string? _lastCompletedDiskName;

    public TrayApplicationContext()
    {
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _settings = _settingsService.Load();
        _monitorService = new DriveMonitorService(_discoveryService);
        _coordinator = new DriveProcessingCoordinator(_settings, new FileMoveService(), _logService, new DriveEjectService());

        _notifyIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = L(UiString.Idle),
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
        _pauseResumeMenuItem = new ToolStripMenuItem(L(UiString.Pause), null, (_, _) => TogglePaused());
        menu.Items.Add(_pauseResumeMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        _openConfigurationMenuItem = new ToolStripMenuItem(L(UiString.OpenConfiguration), null, (_, _) => OpenConfiguration());
        _openDestinationFolderMenuItem = new ToolStripMenuItem(L(UiString.OpenDestinationFolder), null, (_, _) => OpenDestinationFolder());
        menu.Items.Add(_openConfigurationMenuItem);
        menu.Items.Add(_openDestinationFolderMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        _languageMenuItem = new ToolStripMenuItem(L(UiString.Language));
        _englishMenuItem = new ToolStripMenuItem("English", null, (_, _) => ChangeLanguage(AppLanguage.English));
        _ukrainianMenuItem = new ToolStripMenuItem("Українська", null, (_, _) => ChangeLanguage(AppLanguage.Ukrainian));
        _languageMenuItem.DropDownItems.Add(_englishMenuItem);
        _languageMenuItem.DropDownItems.Add(_ukrainianMenuItem);
        menu.Items.Add(_languageMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        _exitMenuItem = new ToolStripMenuItem(L(UiString.Exit), null, (_, _) => ExitThread());
        menu.Items.Add(_exitMenuItem);
        ApplyLanguageToTrayMenu();
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
        ApplyLanguage();

        ProcessConnectedCandidateDrives();
    }

    private void ChangeLanguage(AppLanguage language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        _settings.Language = Localizer.ToLanguageCode(language);
        _settingsService.Save(_settings);
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        ApplyLanguageToTrayMenu();
        _logViewerForm?.ApplyLanguage(_settings);
        UpdateTrayStatus();
    }

    private void ApplyLanguageToTrayMenu()
    {
        if (_pauseResumeMenuItem is not null)
        {
            _pauseResumeMenuItem.Text = _coordinator.IsPaused ? L(UiString.Resume) : L(UiString.Pause);
        }

        if (_openConfigurationMenuItem is not null)
        {
            _openConfigurationMenuItem.Text = L(UiString.OpenConfiguration);
        }

        if (_openDestinationFolderMenuItem is not null)
        {
            _openDestinationFolderMenuItem.Text = L(UiString.OpenDestinationFolder);
        }

        if (_languageMenuItem is not null)
        {
            _languageMenuItem.Text = L(UiString.Language);
        }

        if (_englishMenuItem is not null)
        {
            _englishMenuItem.Checked = CurrentLanguage == AppLanguage.English;
        }

        if (_ukrainianMenuItem is not null)
        {
            _ukrainianMenuItem.Checked = CurrentLanguage == AppLanguage.Ukrainian;
        }

        if (_exitMenuItem is not null)
        {
            _exitMenuItem.Text = L(UiString.Exit);
        }
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
        _lastCompletedDiskName = result.DiskName;
        UpdateTrayStatus();

        if (result.MovedFiles == 0)
        {
            return;
        }

        var body = result.SourceFound
            ? L(UiString.FilesMoved, result.MovedFiles, LogService.FormatBytes(result.MovedBytes))
            : L(UiString.SourceFolderNotFound);

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
            _pauseResumeMenuItem.Text = _coordinator.IsPaused ? L(UiString.Resume) : L(UiString.Pause);
        }

        if (_coordinator.IsPaused)
        {
            _notifyIcon.Icon = _pausedIcon;
            _notifyIcon.Text = TrimTooltip(L(UiString.Paused));
            return;
        }

        var active = _coordinator.Active;
        if (active.Count == 0)
        {
            _notifyIcon.Icon = _idleIcon;
            _notifyIcon.Text = TrimTooltip(_lastCompletedDiskName is null
                ? L(UiString.Idle)
                : L(UiString.CompletedDisk, _lastCompletedDiskName));
            return;
        }

        _notifyIcon.Icon = _copyingIcon;
        _notifyIcon.Text = active.Count == 1
            ? TrimTooltip(L(UiString.ProcessingDiskFiles, active.First().DiskName, active.First().MovedFiles))
            : TrimTooltip(L(UiString.ProcessingDriveCountTooltip, active.Count));
    }

    private static string TrimTooltip(string value) => value.Length <= 63 ? value : value[..63];

    private AppLanguage CurrentLanguage => Localizer.ParseLanguage(_settings.Language);

    private string L(UiString key) => Localizer.Get(CurrentLanguage, key);

    private string L(UiString key, params object[] args) => Localizer.Format(CurrentLanguage, key, args);

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
        _pausedIcon.Dispose();
        base.ExitThreadCore();
    }

    private static Icon LoadApplicationIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "bodycam.ico");
        return File.Exists(path) ? new Icon(path) : IconFactory.CreateIdleIcon();
    }
}
