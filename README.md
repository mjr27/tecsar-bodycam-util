# BodyCamProcessor

Windows tray application that imports files from allowed removable body-camera drives into a dated folder structure.

## Build

Requires .NET 10 SDK on Windows.

```powershell
dotnet build .\BodyCamProcessor.sln
```

## Run

```powershell
dotnet run --project .\BodyCamProcessor\BodyCamProcessor.csproj
```

The app starts in the system tray with no main window. Left-click the tray icon to open the log viewer. Right-click for configuration, destination folder, and exit.

## Configuration

Settings are stored per user at:

```text
%LOCALAPPDATA%\BodyCamProcessor\settings.json
```

Example:

```json
{
  "SourcePath": "\\files\\data\\",
  "DestinationPath": "C:\\Users\\User\\Documents\\BodyCamImports",
  "AllowedDiskNames": [
    "CAM01",
    "CAM02"
  ]
}
```

The configuration window edits the same settings file and can add currently inserted drives to the allowed list. After saving, matching inserted drives are processed automatically.

## Output

Files are moved from:

```text
<Drive>:\files\data\
```

to:

```text
<DestinationPath>\yyyy\MM\yyyy-MM-dd\<DiskName>\
```

Daily logs are appended at:

```text
<DestinationPath>\yyyy\MM\yyyy-MM-dd\log.txt
```
