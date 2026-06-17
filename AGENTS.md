# Agent Notes

This is a .NET 10 Windows Forms tray application.

Key files:

- `BodyCamProcessor/Program.cs`: starts `TrayApplicationContext`; no startup form.
- `BodyCamProcessor/TrayApplicationContext.cs`: owns tray icon, context menu, windows, and service wiring.
- `BodyCamProcessor/Services/DriveMonitorService.cs`: polls removable drives and raises connect/remove events.
- `BodyCamProcessor/Services/DriveProcessingCoordinator.cs`: prevents duplicate processing per connection and starts per-drive background tasks.
- Pause/resume state is owned by `DriveProcessingCoordinator`; all drive start paths should call `TryStart` rather than bypassing it.
- `BodyCamProcessor/Services/FileMoveService.cs`: recursively moves files while preserving structure and skipping existing destination files.
- `BodyCamProcessor/Services/LogService.cs`: thread-safe daily log append/read.
- `BodyCamProcessor/SettingsForm.cs`: WinForms settings UI.
- `BodyCamProcessor/LogViewerForm.cs`: daily log viewer and active import indicator.

Build with:

```powershell
dotnet build .\BodyCamProcessor.sln
```

Avoid changing `.idea` files or generated `bin`/`obj` output. Keep UI work on the WinForms UI thread and file operations off it.
