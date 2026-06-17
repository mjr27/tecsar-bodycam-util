namespace BodyCamProcessor.Models;

public sealed class AppSettings
{
    public string SourcePath { get; set; } = @"\files\data\";

    public string DestinationPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BodyCamImports");

    public List<string> AllowedDiskNames { get; set; } = [];

    public string Language { get; set; } = "en";
}
