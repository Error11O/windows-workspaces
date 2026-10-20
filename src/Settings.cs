using System.Text.Json;
using System.Text.Json.Serialization;

namespace workspaces;

public enum ShortcutModifier { Windows, LeftAlt }

public sealed record AppSettings
{
    public ShortcutModifier Modifier { get; init; } = ShortcutModifier.Windows;
    public int DesktopCount { get; init; } = 10;

    public AppSettings Validate()
    {
        if (!Enum.IsDefined(Modifier) || DesktopCount is < 1 or > 10)
            throw new InvalidDataException("Choose Windows key or Left Alt and a desktop count from 1 to 10.");
        return this;
    }
}

internal sealed class SettingsStore(string path, string? legacyPath = null)
{
    internal static string DefaultPath => Path.Combine(Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData), "workspaces", "settings.json");
    internal static string LegacyPath => Path.Combine(Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData), "WorkspaceManager", "settings.json");
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    internal AppSettings Load()
    {
        var loadPath = File.Exists(path) ? path : legacyPath;
        return loadPath is null || !File.Exists(loadPath) ? new AppSettings() :
            (JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(loadPath), Options)
                ?? throw new InvalidDataException("The settings file is empty.")).Validate();
    }

    internal void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings.Validate(), Options);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, json);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
