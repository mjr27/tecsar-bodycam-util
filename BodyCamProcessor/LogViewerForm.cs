using BodyCamProcessor.Localization;
using BodyCamProcessor.Models;
using BodyCamProcessor.Services;

namespace BodyCamProcessor;

public sealed class LogViewerForm : Form
{
    private const int ProgressRowHeight = 48;
    private const int MaxProgressPanelHeight = 220;

    private AppSettings _settings;
    private readonly LogService _logService;
    private readonly DriveProcessingCoordinator _coordinator;
    private readonly DateTimePicker _datePicker = new();
    private readonly TextBox _logTextBox = new();
    private readonly TableLayoutPanel _progressTable = new();
    private readonly Label _dateLabel = new();
    private readonly Label _statusLabel = new();
    private readonly System.Windows.Forms.Timer _logReloadDebounceTimer = new() { Interval = 250 };
    private FileSystemWatcher? _logWatcher;
    private string _loadedLogPath = string.Empty;

    public LogViewerForm(AppSettings settings, LogService logService, DriveProcessingCoordinator coordinator)
    {
        _settings = settings;
        _logService = logService;
        _coordinator = coordinator;

        UpdateTitle();
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 520);
        MinimumSize = new Size(560, 360);

        BuildUi();
        LoadLog();
        UpdateProgress();

        _coordinator.StateChanged += CoordinatorOnStateChanged;
        _logReloadDebounceTimer.Tick += (_, _) =>
        {
            _logReloadDebounceTimer.Stop();
            LoadLog();
        };
        ConfigureLogWatcher();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 4,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var previousDayButton = new Button { Text = "<", Width = 34 };
        previousDayButton.Click += (_, _) => _datePicker.Value = _datePicker.Value.Date.AddDays(-1);
        var nextDayButton = new Button { Text = ">", Width = 34 };
        nextDayButton.Click += (_, _) => _datePicker.Value = _datePicker.Value.Date.AddDays(1);
        _datePicker.Format = DateTimePickerFormat.Short;
        _datePicker.Value = DateTime.Today;
        _datePicker.ValueChanged += (_, _) => LoadLog();
        _dateLabel.Text = L(UiString.Date);
        _dateLabel.AutoSize = true;
        _dateLabel.Padding = new Padding(0, 7, 6, 0);
        topPanel.Controls.Add(_dateLabel);
        topPanel.Controls.Add(previousDayButton);
        topPanel.Controls.Add(_datePicker);
        topPanel.Controls.Add(nextDayButton);
        root.Controls.Add(topPanel, 0, 0);

        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Both;
        _logTextBox.WordWrap = false;
        _logTextBox.Font = new Font(FontFamily.GenericMonospace, 10);
        root.Controls.Add(_logTextBox, 0, 1);

        _progressTable.AutoSize = true;
        _progressTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _progressTable.AutoScroll = true;
        _progressTable.Dock = DockStyle.Top;
        _progressTable.MaximumSize = new Size(0, MaxProgressPanelHeight);
        _progressTable.Visible = false;
        _progressTable.ColumnCount = 1;
        _progressTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(_progressTable, 0, 2);

        _statusLabel.Dock = DockStyle.Fill;
        root.Controls.Add(_statusLabel, 0, 3);

        Controls.Add(root);
    }

    private void LoadLog()
    {
        _loadedLogPath = GetSelectedLogPath();
        _logTextBox.Text = _logService.ReadLog(_settings, _datePicker.Value.Date);
    }

    public void ApplyLanguage(AppSettings settings)
    {
        _settings = settings;
        UpdateTitle();
        _dateLabel.Text = L(UiString.Date);
        LoadLog();
        ConfigureLogWatcher();
        UpdateProgress();
    }

    private void ConfigureLogWatcher()
    {
        _logWatcher?.Dispose();
        _logWatcher = null;

        if (string.IsNullOrWhiteSpace(_settings.DestinationPath))
        {
            return;
        }

        Directory.CreateDirectory(_settings.DestinationPath);
        _logWatcher = new FileSystemWatcher(_settings.DestinationPath, "log.txt")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.CreationTime |
                           NotifyFilters.FileName |
                           NotifyFilters.LastWrite |
                           NotifyFilters.Size
        };
        _logWatcher.Changed += LogWatcherOnChanged;
        _logWatcher.Created += LogWatcherOnChanged;
        _logWatcher.Deleted += LogWatcherOnChanged;
        _logWatcher.Renamed += LogWatcherOnRenamed;
        _logWatcher.EnableRaisingEvents = true;
    }

    private void LogWatcherOnChanged(object sender, FileSystemEventArgs e)
    {
        if (IsSelectedLogPath(e.FullPath))
        {
            QueueLogReload();
        }
    }

    private void LogWatcherOnRenamed(object sender, RenamedEventArgs e)
    {
        if (IsSelectedLogPath(e.FullPath) || IsSelectedLogPath(e.OldFullPath))
        {
            QueueLogReload();
        }
    }

    private bool IsSelectedLogPath(string path) =>
        string.Equals(path, _loadedLogPath, StringComparison.OrdinalIgnoreCase);

    private void QueueLogReload()
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(QueueLogReload);
            return;
        }

        _logReloadDebounceTimer.Stop();
        _logReloadDebounceTimer.Start();
    }

    private string GetSelectedLogPath() =>
        Path.Combine(LogService.GetDayFolder(_settings.DestinationPath, _datePicker.Value.Date), "log.txt");

    private void CoordinatorOnStateChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(UpdateProgress);
        }
        else
        {
            UpdateProgress();
        }
    }

    private void UpdateProgress()
    {
        var active = _coordinator.Active
            .OrderBy(progress => progress.DiskName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _progressTable.SuspendLayout();
        try
        {
            _progressTable.Visible = active.Count > 0;
            _progressTable.Controls.Clear();
            _progressTable.RowStyles.Clear();
            _progressTable.RowCount = active.Count;

            for (var index = 0; index < active.Count; index++)
            {
                _progressTable.RowStyles.Add(new RowStyle(SizeType.Absolute, ProgressRowHeight));
                _progressTable.Controls.Add(CreateProgressRow(active[index], CurrentLanguage), 0, index);
            }
        }
        finally
        {
            _progressTable.ResumeLayout();
        }

        if (active.Count == 0)
        {
            _statusLabel.Text = L(UiString.Idle);
            return;
        }

        _statusLabel.Text = active.Count == 1
            ? L(
                UiString.ProcessingDiskStatus,
                active.First().DiskName,
                active.First().MovedFiles,
                LogService.FormatBytes(active.First().MovedBytes))
            : L(UiString.ProcessingDriveCount, active.Count);
    }

    private static Control CreateProgressRow(DriveProcessingProgress progress, AppLanguage language)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 6)
        };
        row.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        row.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));

        var label = new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Text = FormatProgressText(progress, language)
        };
        row.Controls.Add(label, 0, 0);

        var progressBar = new InstantProgressBar
        {
            Dock = DockStyle.Fill,
            Style = ProgressBarStyle.Blocks,
            Minimum = 0,
            Maximum = 10000
        };
        progressBar.SetInstantValue(CalculateProgressBarValue(progress));
        row.Controls.Add(progressBar, 0, 1);

        return row;
    }

    private static string FormatProgressText(DriveProcessingProgress progress, AppLanguage language)
    {
        var copied = LogService.FormatBytes(progress.DestinationBytes);
        var total = LogService.FormatBytes(progress.TotalBytes);
        var currentFile = string.IsNullOrWhiteSpace(progress.CurrentFile) ? string.Empty : $" | {progress.CurrentFile}";
        return $"{progress.DiskName}: {copied} / {total} | {Localizer.Format(language, UiString.ProgressFilesMoved, progress.MovedFiles)}{currentFile}";
    }

    private static int CalculateProgressBarValue(DriveProcessingProgress progress)
    {
        if (progress.TotalBytes <= 0)
        {
            return 0;
        }

        var value = (int)Math.Round(progress.DestinationBytes * 10000d / progress.TotalBytes);
        return Math.Clamp(value, 0, 10000);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logReloadDebounceTimer.Stop();
            _logReloadDebounceTimer.Dispose();
            _logWatcher?.Dispose();
            _coordinator.StateChanged -= CoordinatorOnStateChanged;
        }

        base.Dispose(disposing);
    }

    private AppLanguage CurrentLanguage => Localizer.ParseLanguage(_settings.Language);

    private void UpdateTitle()
    {
        Text = $"{L(UiString.LogsTitle)} {AppVersion.DisplayVersion}";
    }

    private string L(UiString key) => Localizer.Get(CurrentLanguage, key);

    private string L(UiString key, params object[] args) => Localizer.Format(CurrentLanguage, key, args);
}
