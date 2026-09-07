namespace OpenOsk.Core.Prediction;

/// <summary>
/// Follows what the user is typing on the on-screen keyboard to know the current word prefix.
/// It only sees keys sent by OpenOSK itself, so it never reads other applications' text.
/// </summary>
public sealed class TypedWordTracker
{
    private readonly System.Text.StringBuilder _word = new();

    public event EventHandler<string>? WordCompleted;

    public string CurrentPrefix => _word.ToString();

    public bool HasPrefix => _word.Length > 0;

    public void OnCharacter(char c)
    {
        if (char.IsLetter(c) || c == '\'')
        {
            _word.Append(c);
        }
        else
        {
            Break();
        }
    }

    public void OnText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        foreach (var c in text)
        {
            OnCharacter(c);
        }
    }

    public void OnBackspace()
    {
        if (_word.Length > 0)
        {
            _word.Length--;
        }
    }

    /// <summary>Ends the current word (space, enter, navigation, click elsewhere).</summary>
    public void Break()
    {
        if (_word.Length > 0)
        {
            var word = _word.ToString();
            _word.Clear();
            WordCompleted?.Invoke(this, word);
        }
    }

    /// <summary>Discard the prefix without reporting a completed word (e.g. after Esc or focus change).</summary>
    public void Reset() => _word.Clear();
}
