namespace BodyCamProcessor.Services;

public sealed class DriveEjectService
{
    public Task TryEjectAsync(string driveRoot)
    {
        return Task.Run(() =>
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType is null)
                {
                    return;
                }

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic computer = shell.NameSpace(17);
                dynamic drive = computer.ParseName(driveRoot.TrimEnd('\\'));
                if (drive is null)
                {
                    return;
                }

                foreach (dynamic verb in drive.Verbs())
                {
                    var name = ((string)verb.Name).Replace("&", string.Empty, StringComparison.Ordinal);
                    if (name.Contains("Eject", StringComparison.OrdinalIgnoreCase))
                    {
                        verb.DoIt();
                        break;
                    }
                }
            }
            catch
            {
                // Eject support depends on Windows shell/device state; failures are non-fatal.
            }
        });
    }
}
