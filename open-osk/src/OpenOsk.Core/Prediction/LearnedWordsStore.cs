namespace OpenOsk.Core.Prediction;

/// <summary>Persists learned words as "word\tcount" lines in the user's local profile.</summary>
public sealed class LearnedWordsStore
{
    public LearnedWordsStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    public string FilePath { get; }

    public IReadOnlyList<KeyValuePair<string, int>> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return [];
            }

            var result = new List<KeyValuePair<string, int>>();
            foreach (var line in File.ReadLines(FilePath))
            {
                var tab = line.IndexOf('\t');
                if (tab <= 0)
                {
                    continue;
                }

                if (int.TryParse(line[(tab + 1)..], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var count))
                {
                    result.Add(new KeyValuePair<string, int>(line[..tab], count));
                }
            }

            return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    public void Save(IEnumerable<KeyValuePair<string, int>> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = FilePath + ".tmp";
        File.WriteAllLines(temp, words.Select(w => $"{w.Key}\t{w.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        File.Move(temp, FilePath, overwrite: true);
    }
}
