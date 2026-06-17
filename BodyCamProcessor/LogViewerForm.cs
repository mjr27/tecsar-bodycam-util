using BodyCamProcessor.Models;
using BodyCamProcessor.Services;

namespace BodyCamProcessor;

public sealed class LogViewerForm : Form
{
    private readonly AppSettings _settings;
    private readonly LogService _logService;
    private readonly DriveProcessingCoordinator _coordinator;
    private readonly DateTimePicker _datePicker = new();
    private readonly TextBox _logTextBox = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _statusLabel = new();

    public LogViewerForm(AppSettings settings, LogService logService, DriveProcessingCoordinator coordinator)
    {
        _settings = settings;
        _logService = logService;
        _coordinator = coordinator;

        Text = "BodyCamProcessor Logs";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 520);
        MinimumSize = new Size(560, 360);

        BuildUi();
        LoadLog();
        UpdateProgress();

        _coordinator.StateChanged += CoordinatorOnStateChanged;
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var previousDayButton = new Button { Text = "<", Width = 34 };
        previousDayButton.Click += (_, _) => _datePicker.Value = _datePicker.Value.Date.AddDays(-1);
        var nextDayButton = new Button { Text = ">", Width = 34 };
        nextDayButton.Click += (_, _) => _datePicker.Value = _datePicker.Value.Date.AddDays(1);
        _datePicker.Format = DateTimePickerFormat.Short;
        _datePicker.Value = DateTime.Today;
        _datePicker.ValueChanged += (_, _) => LoadLog();
        topPanel.Controls.Add(new Label { Text = "Date", AutoSize = true, Padding = new Padding(0, 7, 6, 0) });
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

        _progressBar.Dock = DockStyle.Fill;
        root.Controls.Add(_progressBar, 0, 2);

        _statusLabel.Dock = DockStyle.Fill;
        root.Controls.Add(_statusLabel, 0, 3);

        Controls.Add(root);
    }

    private void LoadLog()
    {
        _logTextBox.Text = _logService.ReadLog(_settings, _datePicker.Value.Date);
    }

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
        var active = _coordinator.Active;
        if (active.Count == 0)
        {
            _progressBar.Style = ProgressBarStyle.Blocks;
            _progressBar.Value = 0;
            _statusLabel.Text = "Idle";
            return;
        }

        _progressBar.Style = ProgressBarStyle.Marquee;
        _statusLabel.Text = active.Count == 1
            ? $"Processing {active.First().DiskName}: {active.First().MovedFiles} files, {LogService.FormatBytes(active.First().MovedBytes)}"
            : $"Processing {active.Count} drives";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _coordinator.StateChanged -= CoordinatorOnStateChanged;
        }

        base.Dispose(disposing);
    }
}
