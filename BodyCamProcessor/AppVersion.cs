using System.Reflection;

namespace BodyCamProcessor;

public static class AppVersion
{
    private const string DefaultDisplayVersion = "v0.0.0-local";

    public static string DisplayVersion { get; } = GetDisplayVersion();

    private static string GetDisplayVersion()
    {
        var version = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        return string.IsNullOrWhiteSpace(version) ? DefaultDisplayVersion : version;
    }
}
