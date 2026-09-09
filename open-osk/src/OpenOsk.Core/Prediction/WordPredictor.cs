namespace OpenOsk.Core.Prediction;

/// <summary>
/// Prefix-based word prediction over a frequency lexicon plus words learned from the user. Everything
/// stays in memory and on the local disk; nothing is ever sent anywhere.
/// </summary>
public sealed class WordPredictor
{
    private readonly Dictionary<string, int> _frequency = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _learned = new(StringComparer.OrdinalIgnoreCase);
    private string[] _sorted = [];
    private bool _dirty = true;

    /// <summary>Frequency bonus given to every learned word so recent personal vocabulary ranks first.</summary>
    public int LearnedWeight { get; set; } = 1_000_000;

    /// <summary>Minimum length for a typed word to be learned.</summary>
    public int MinLearnLength { get; set; } = 3;

    public int WordCount => _frequency.Count;

    public IReadOnlyDictionary<string, int> LearnedWords => _learned;

    /// <summary>
    /// Loads a lexicon: one word per line, optional tab-separated frequency. Lines without a frequency
    /// are ranked by position (earlier is more frequent). Lines starting with '#' are ignored.
    /// </summary>
    public void LoadLexicon(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var rank = 0;
        var lines = new List<(string Word, int? Freq)>();
        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var tab = line.IndexOf('\t');
            if (tab < 0)
            {
                lines.Add((line, null));
            }
            else if (int.TryParse(line[(tab + 1)..], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var freq))
            {
                lines.Add((line[..tab].Trim(), freq));
            }
            else
            {
                lines.Add((line[..tab].Trim(), null));
            }
        }

        foreach (var (word, freq) in lines)
        {
            var value = freq ?? Math.Max(1, lines.Count - rank);
            rank++;
            if (!_frequency.TryGetValue(word, out var existing) || existing < value)
            {
                _frequency[word] = value;
            }
        }

        _dirty = true;
    }

    public void LoadLexicon(string text)
    {
        using var reader = new StringReader(text);
        LoadLexicon(reader);
    }

    /// <summary>Restores previously learned words, e.g. from the user's local profile.</summary>
    public void LoadLearned(IEnumerable<KeyValuePair<string, int>> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        foreach (var (word, count) in words)
        {
            if (IsLearnable(word))
            {
                _learned[word] = Math.Max(count, _learned.GetValueOrDefault(word));
            }
        }

        _dirty = true;
    }

    /// <summary>Records that the user typed <paramref name="word"/>.</summary>
    public void Learn(string word)
    {
        if (!IsLearnable(word))
        {
            return;
        }

        _learned[word] = _learned.GetValueOrDefault(word) + 1;
        _dirty = true;
    }

    public void ForgetAll()
    {
        _learned.Clear();
        _dirty = true;
    }

    /// <summary>Top <paramref name="max"/> words starting with <paramref name="prefix"/>, best first.</summary>
    public IReadOnlyList<string> Predict(string prefix, int max = 5)
    {
        if (string.IsNullOrEmpty(prefix) || max <= 0)
        {
            return [];
        }

        EnsureIndex();
        var start = LowerBound(prefix);
        var candidates = new List<(string Word, int Score)>();
        for (var i = start; i < _sorted.Length; i++)
        {
            var word = _sorted[i];
            if (!word.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (word.Length == prefix.Length)
            {
                continue;
            }

            candidates.Add((word, Score(word)));
        }

        return candidates
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Word.Length)
            .ThenBy(c => c.Word, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(c => MatchCase(c.Word, prefix))
            .ToArray();
    }

    /// <summary>The characters that must be typed to turn <paramref name="prefix"/> into <paramref name="word"/>.</summary>
    public static string Completion(string prefix, string word)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(word);
        return word.Length > prefix.Length && word.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? word[prefix.Length..]
            : word;
    }

    /// <summary>Applies the user's capitalisation: "Th" → "This", "TH" → "THIS", "th" → "this".</summary>
    public static string MatchCase(string word, string typedPrefix)
    {
        ArgumentNullException.ThrowIfNull(word);
        if (string.IsNullOrEmpty(typedPrefix))
        {
            return word;
        }

        if (typedPrefix.Length > 1 && typedPrefix.All(char.IsUpper))
        {
            return word.ToUpperInvariant();
        }

        if (char.IsUpper(typedPrefix[0]))
        {
            return char.ToUpperInvariant(word[0]) + word[1..];
        }

        return word;
    }

    private bool IsLearnable(string? word) =>
        !string.IsNullOrWhiteSpace(word) && word.Length >= MinLearnLength && word.All(char.IsLetter);

    private int Score(string word)
    {
        var score = _frequency.GetValueOrDefault(word);
        if (_learned.TryGetValue(word, out var learned))
        {
            score += LearnedWeight + learned;
        }

        return score;
    }

    private void EnsureIndex()
    {
        if (!_dirty)
        {
            return;
        }

        _sorted = _frequency.Keys.Union(_learned.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _dirty = false;
    }

    private int LowerBound(string prefix)
    {
        int lo = 0, hi = _sorted.Length;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (string.Compare(_sorted[mid], prefix, StringComparison.OrdinalIgnoreCase) < 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }
}
