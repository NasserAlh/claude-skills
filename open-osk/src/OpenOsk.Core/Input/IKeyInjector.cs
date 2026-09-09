namespace OpenOsk.Core.Input;

/// <summary>Abstraction over the OS input queue so the core can be tested without Windows.</summary>
public interface IKeyInjector
{
    /// <summary>Sends the strokes as one atomic batch, in order.</summary>
    void Send(IReadOnlyList<KeyStroke> strokes);

    /// <summary>Types literal text, independent of the active keyboard layout.</summary>
    void SendText(string text);
}
