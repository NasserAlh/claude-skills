namespace OpenOsk.Core.Prediction;

public static class BuiltInLexicons
{
    /// <summary>Reads an embedded lexicon by language tag (e.g. "en"). Returns null if none exists.</summary>
    public static string? Read(string language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        var assembly = typeof(BuiltInLexicons).Assembly;
        var name = $"OpenOsk.Core.Lexicons.{language.ToLowerInvariant()}.txt";
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static IReadOnlyList<string> Available()
    {
        const string prefix = "OpenOsk.Core.Lexicons.";
        return typeof(BuiltInLexicons).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal) && n.EndsWith(".txt", StringComparison.Ordinal))
            .Select(n => n[prefix.Length..^4])
            .ToArray();
    }
}
