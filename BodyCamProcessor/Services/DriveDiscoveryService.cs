using BodyCamProcessor.Models;

namespace BodyCamProcessor.Services;

public sealed class DriveDiscoveryService
{
    public IReadOnlyList<DriveSnapshot> GetCandidateDrives()
    {
        var drives = new List<DriveSnapshot>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!IsCandidateDriveType(drive.DriveType) || !drive.IsReady)
            {
                continue;
            }

            string label;
            try
            {
                label = drive.VolumeLabel;
            }
            catch
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(label))
            {
                drives.Add(new DriveSnapshot(drive.RootDirectory.FullName, label));
            }
        }

        return drives;
    }

    private static bool IsCandidateDriveType(DriveType driveType) =>
        driveType is DriveType.Removable or DriveType.Fixed;
}
