using BodyCamProcessor.Models;
using BodyCamProcessor.Services;

namespace BodyCamProcessor;

public sealed class SettingsForm : Form
{
    private static readonly Size StandardButtonSize = new(96, 32);
    private readonly DriveDiscoveryService _driveDiscoveryService;
    private readonly TextBox _sourcePathTextBox = new();
    private readonly TextBox _destinationPathTextBox = new();
    private readonly ListBox _allowedDiskNamesListBox = new();
    private readonly ComboBox _connectedDrivesComboBox = new();
    private readonly System.Windows.Forms.Timer _connectedDrivesRefreshTimer = new() { Interval = 2000 };
    private List<string> _connectedDriveItems = [];

    public SettingsForm(AppSettings settings, DriveDiscoveryService driveDiscoveryService)
    {
        _driveDiscoveryService = driveDiscoveryService;
        Settings = new AppSettings
        {
            SourcePath = settings.SourcePath,
            DestinationPath = settings.DestinationPath,
            AllowedDiskNames = settings.AllowedDiskNames.ToList()
        };

        Text = "BodyCamProcessor Configuration";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(620, 440);

        BuildUi();
        LoadValues();

        _connectedDrivesRefreshTimer.Tick += (_, _) => RefreshConnectedDrives();
        _connectedDrivesRefreshTimer.Start();
    }

    public AppSettings Settings { get; private set; }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 3,
            RowCount = 8
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        root.Controls.Add(new Label { Text = "Source path", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _sourcePathTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        root.Controls.Add(_sourcePathTextBox, 1, 0);
        root.SetColumnSpan(_sourcePathTextBox, 2);

        root.Controls.Add(new Label { Text = "Destination", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _destinationPathTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        root.Controls.Add(_destinationPathTextBox, 1, 1);
        var browseButton = CreateButton("Browse");
        browseButton.Anchor = AnchorStyles.Right;
        browseButton.Click += (_, _) => BrowseDestination();
        root.Controls.Add(browseButton, 2, 1);

        root.Controls.Add(new Label { Text = "Allowed disks", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        _allowedDiskNamesListBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
        root.Controls.Add(_allowedDiskNamesListBox, 1, 2);
        root.SetColumnSpan(_allowedDiskNamesListBox, 2);
        root.SetRowSpan(_allowedDiskNamesListBox, 3);

        var addPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };
        var manualNameTextBox = new TextBox { Width = 220, Margin = new Padding(3, 3, 10, 3) };
        var addManualButton = CreateButton("Add");
        addManualButton.Click += (_, _) => AddDiskName(manualNameTextBox.Text);
        var removeButton = CreateButton("Remove");
        removeButton.Click += (_, _) => RemoveSelectedDiskName();
        addPanel.Controls.Add(manualNameTextBox);
        addPanel.Controls.Add(addManualButton);
        addPanel.Controls.Add(removeButton);
        root.Controls.Add(addPanel, 1, 5);
        root.SetColumnSpan(addPanel, 2);

        root.Controls.Add(new Label { Text = "Inserted drive", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 6);
        _connectedDrivesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _connectedDrivesComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        root.Controls.Add(_connectedDrivesComboBox, 1, 6);
        var addInsertedButton = CreateButton("Add Drive");
        addInsertedButton.Anchor = AnchorStyles.Right;
        addInsertedButton.Click += (_, _) => AddSelectedInsertedDrive();
        root.Controls.Add(addInsertedButton, 2, 6);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 4, 0, 0),
            WrapContents = false
        };
        var saveButton = CreateButton("Save");
        saveButton.DialogResult = DialogResult.OK;
        var cancelButton = CreateButton("Cancel");
        cancelButton.DialogResult = DialogResult.Cancel;
        saveButton.Click += (_, _) => SaveValues();
        buttonsPanel.Controls.Add(saveButton);
        buttonsPanel.Controls.Add(cancelButton);
        root.Controls.Add(buttonsPanel, 0, 7);
        root.SetColumnSpan(buttonsPanel, 3);

        for (var row = 0; row < root.RowCount; row++)
        {
            root.RowStyles.Add(row is 2 or 3 or 4 ? new RowStyle(SizeType.Percent, 33) : new RowStyle(SizeType.Absolute, 48));
        }

        Controls.Add(root);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static Button CreateButton(string text) => new()
    {
        Text = text,
        Size = StandardButtonSize,
        MinimumSize = StandardButtonSize,
        AutoSize = false,
        Margin = new Padding(4)
    };

    private void LoadValues()
    {
        _sourcePathTextBox.Text = Settings.SourcePath;
        _destinationPathTextBox.Text = Settings.DestinationPath;
        RefreshAllowedDiskNames();
        RefreshConnectedDrives();
    }

    private void RefreshAllowedDiskNames()
    {
        _allowedDiskNamesListBox.Items.Clear();
        foreach (var diskName in Settings.AllowedDiskNames.Order(StringComparer.OrdinalIgnoreCase))
        {
            _allowedDiskNamesListBox.Items.Add(diskName);
        }
    }

    private void RefreshConnectedDrives()
    {
        var selectedItem = _connectedDrivesComboBox.SelectedItem as string;
        var driveItems = _driveDiscoveryService.GetCandidateDrives()
            .Select(drive => $"{drive.VolumeLabel} ({drive.RootPath})")
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (driveItems.SequenceEqual(_connectedDriveItems, StringComparer.Ordinal))
        {
            return;
        }

        _connectedDriveItems = driveItems;
        _connectedDrivesComboBox.BeginUpdate();
        try
        {
            _connectedDrivesComboBox.Items.Clear();
            foreach (var driveItem in driveItems)
            {
                _connectedDrivesComboBox.Items.Add(driveItem);
            }

            if (selectedItem is not null && driveItems.Contains(selectedItem, StringComparer.Ordinal))
            {
                _connectedDrivesComboBox.SelectedItem = selectedItem;
            }
            else if (_connectedDrivesComboBox.Items.Count > 0)
            {
                _connectedDrivesComboBox.SelectedIndex = 0;
            }
        }
        finally
        {
            _connectedDrivesComboBox.EndUpdate();
        }
    }

    private void BrowseDestination()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select destination folder",
            SelectedPath = Directory.Exists(_destinationPathTextBox.Text) ? _destinationPathTextBox.Text : string.Empty
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _destinationPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void AddDiskName(string value)
    {
        var diskName = value.Trim();
        if (string.IsNullOrWhiteSpace(diskName) ||
            Settings.AllowedDiskNames.Contains(diskName, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        Settings.AllowedDiskNames.Add(diskName);
        RefreshAllowedDiskNames();
    }

    private void AddSelectedInsertedDrive()
    {
        if (_connectedDrivesComboBox.SelectedItem is not string selected)
        {
            return;
        }

        var diskName = selected.Split(" (", StringSplitOptions.None)[0];
        AddDiskName(diskName);
    }

    private void RemoveSelectedDiskName()
    {
        if (_allowedDiskNamesListBox.SelectedItem is not string selected)
        {
            return;
        }

        Settings.AllowedDiskNames.RemoveAll(name => string.Equals(name, selected, StringComparison.OrdinalIgnoreCase));
        RefreshAllowedDiskNames();
    }

    private void SaveValues()
    {
        Settings = new AppSettings
        {
            SourcePath = string.IsNullOrWhiteSpace(_sourcePathTextBox.Text) ? @"\files\data\" : _sourcePathTextBox.Text.Trim(),
            DestinationPath = string.IsNullOrWhiteSpace(_destinationPathTextBox.Text)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BodyCamImports")
                : _destinationPathTextBox.Text.Trim(),
            AllowedDiskNames = Settings.AllowedDiskNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connectedDrivesRefreshTimer.Stop();
            _connectedDrivesRefreshTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
