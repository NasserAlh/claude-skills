using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenOsk.Core.Settings;

/// <summary>
/// Loads and saves <see cref="OskSettings"/> as human-readable JSON. A corrupt or missing file yields
/// defaults instead of an exception so the keyboard always starts. Saves are atomic (write, then rename).
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public SettingsStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    public string FilePath { get; }

    public OskSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new OskSettings();
            }

            var json = File.ReadAllText(FilePath);
            return Deserialize(json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new OskSettings();
        }
    }

    public void Save(OskSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, Serialize(settings));
        File.Move(temp, FilePath, overwrite: true);
    }

    public static string Serialize(OskSettings settings) =>
        JsonSerializer.Serialize(settings.Normalized(), Options);

    public static OskSettings Deserialize(string json) =>
        (JsonSerializer.Deserialize<OskSettings>(json, Options) ?? new OskSettings()).Normalized();
}
