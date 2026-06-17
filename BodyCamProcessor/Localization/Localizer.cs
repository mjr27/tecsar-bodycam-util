using System.Globalization;

namespace BodyCamProcessor.Localization;

public enum UiString
{
    Add,
    AddDrive,
    AllowedDisks,
    Browse,
    Cancel,
    CompletedDisk,
    ConfigurationTitle,
    Date,
    Destination,
    Error,
    Exit,
    Files,
    FilesMoved,
    Idle,
    InsertedDrive,
    Language,
    LogsTitle,
    NoLogForSelectedDate,
    OpenConfiguration,
    OpenDestinationFolder,
    Pause,
    Paused,
    ProcessingDiskFiles,
    ProcessingDiskStatus,
    ProcessingDriveCount,
    ProcessingDriveCountTooltip,
    ProgressFilesMoved,
    Remove,
    Resume,
    Save,
    SelectDestinationFolder,
    SourceFolderNotFound,
    SourcePath
}

public static class Localizer
{
    private const string EnglishCode = "en";
    private const string UkrainianCode = "uk";

    private static readonly Dictionary<UiString, string> English = new()
    {
        [UiString.Add] = "Add",
        [UiString.AddDrive] = "Add Drive",
        [UiString.AllowedDisks] = "Allowed disks",
        [UiString.Browse] = "Browse",
        [UiString.Cancel] = "Cancel",
        [UiString.CompletedDisk] = "Completed {0}",
        [UiString.ConfigurationTitle] = "BodyCamProcessor Configuration",
        [UiString.Date] = "Date",
        [UiString.Destination] = "Destination",
        [UiString.Error] = "ERROR",
        [UiString.Exit] = "Exit",
        [UiString.Files] = "files",
        [UiString.FilesMoved] = "{0} files moved ({1}).",
        [UiString.Idle] = "Idle",
        [UiString.InsertedDrive] = "Inserted drive",
        [UiString.Language] = "Language",
        [UiString.LogsTitle] = "BodyCamProcessor Logs",
        [UiString.NoLogForSelectedDate] = "No log exists for the selected date.",
        [UiString.OpenConfiguration] = "Open Configuration",
        [UiString.OpenDestinationFolder] = "Open Destination Folder",
        [UiString.Pause] = "Pause",
        [UiString.Paused] = "Paused",
        [UiString.ProcessingDiskFiles] = "Processing {0}... {1} files",
        [UiString.ProcessingDiskStatus] = "Processing {0}: {1} files, {2}",
        [UiString.ProcessingDriveCount] = "Processing {0} drives",
        [UiString.ProcessingDriveCountTooltip] = "Processing {0} drives...",
        [UiString.ProgressFilesMoved] = "{0} files moved",
        [UiString.Remove] = "Remove",
        [UiString.Resume] = "Resume",
        [UiString.Save] = "Save",
        [UiString.SelectDestinationFolder] = "Select destination folder",
        [UiString.SourceFolderNotFound] = "Source folder was not found; no files moved.",
        [UiString.SourcePath] = "Source path"
    };

    private static readonly Dictionary<UiString, string> Ukrainian = new()
    {
        [UiString.Add] = "Додати",
        [UiString.AddDrive] = "Додати диск",
        [UiString.AllowedDisks] = "Дозволені диски",
        [UiString.Browse] = "Огляд",
        [UiString.Cancel] = "Скасувати",
        [UiString.CompletedDisk] = "Завершено {0}",
        [UiString.ConfigurationTitle] = "Налаштування BodyCamProcessor",
        [UiString.Date] = "Дата",
        [UiString.Destination] = "Призначення",
        [UiString.Error] = "ПОМИЛКА",
        [UiString.Exit] = "Вийти",
        [UiString.Files] = "файлів",
        [UiString.FilesMoved] = "Переміщено файлів: {0} ({1}).",
        [UiString.Idle] = "Очікування",
        [UiString.InsertedDrive] = "Вставлений диск",
        [UiString.Language] = "Мова",
        [UiString.LogsTitle] = "Журнали BodyCamProcessor",
        [UiString.NoLogForSelectedDate] = "Для вибраної дати журнал відсутній.",
        [UiString.OpenConfiguration] = "Відкрити налаштування",
        [UiString.OpenDestinationFolder] = "Відкрити папку призначення",
        [UiString.Pause] = "Пауза",
        [UiString.Paused] = "Призупинено",
        [UiString.ProcessingDiskFiles] = "Обробка {0}... файлів: {1}",
        [UiString.ProcessingDiskStatus] = "Обробка {0}: файлів: {1}, {2}",
        [UiString.ProcessingDriveCount] = "Обробка дисків: {0}",
        [UiString.ProcessingDriveCountTooltip] = "Обробка дисків: {0}...",
        [UiString.ProgressFilesMoved] = "переміщено файлів: {0}",
        [UiString.Remove] = "Видалити",
        [UiString.Resume] = "Відновити",
        [UiString.Save] = "Зберегти",
        [UiString.SelectDestinationFolder] = "Виберіть папку призначення",
        [UiString.SourceFolderNotFound] = "Папку джерела не знайдено; файли не переміщено.",
        [UiString.SourcePath] = "Шлях джерела"
    };

    public static AppLanguage ParseLanguage(string? languageCode) =>
        string.Equals(languageCode, UkrainianCode, StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Ukrainian
            : AppLanguage.English;

    public static string ToLanguageCode(AppLanguage language) =>
        language == AppLanguage.Ukrainian ? UkrainianCode : EnglishCode;

    public static string Get(AppLanguage language, UiString key) =>
        GetDictionary(language).TryGetValue(key, out var value) ? value : English[key];

    public static string Format(AppLanguage language, UiString key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(language, key), args);

    public static string Get(string? languageCode, UiString key) =>
        Get(ParseLanguage(languageCode), key);

    public static string Format(string? languageCode, UiString key, params object[] args) =>
        Format(ParseLanguage(languageCode), key, args);

    private static Dictionary<UiString, string> GetDictionary(AppLanguage language) =>
        language == AppLanguage.Ukrainian ? Ukrainian : English;
}
