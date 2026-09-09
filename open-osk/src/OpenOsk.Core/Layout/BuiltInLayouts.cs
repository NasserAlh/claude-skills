namespace OpenOsk.Core.Layout;

/// <summary>Layouts embedded in the core assembly. User layouts can be loaded from disk with <see cref="LayoutParser"/>.</summary>
public static class BuiltInLayouts
{
    public const string StandardId = "standard";
    public const string NavigationId = "navigation";

    private static readonly Lazy<IReadOnlyDictionary<string, KeyboardLayout>> Cache = new(LoadAll);

    public static IReadOnlyDictionary<string, KeyboardLayout> All => Cache.Value;

    public static KeyboardLayout Standard => All[StandardId];

    public static KeyboardLayout Navigation => All[NavigationId];

    public static KeyboardLayout Get(string id) =>
        All.TryGetValue(id, out var layout) ? layout : Standard;

    private static IReadOnlyDictionary<string, KeyboardLayout> LoadAll()
    {
        var assembly = typeof(BuiltInLayouts).Assembly;
        var result = new Dictionary<string, KeyboardLayout>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith("OpenOsk.Core.Layouts.", StringComparison.Ordinal) || !name.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            var layout = LayoutParser.Parse(reader.ReadToEnd());
            result[layout.Id] = layout;
        }

        return result;
    }
}
