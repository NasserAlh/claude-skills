namespace OpenOsk.Core.Typing;

/// <summary>
/// Row/column switch scanning. Rows are highlighted in turn; one press selects a row; keys in that row
/// are then highlighted in turn; a second press selects the key. If a row is swept
/// <see cref="MaxRowPasses"/> times without a selection, scanning returns to the rows.
/// </summary>
public sealed class ScanController
{
    private IReadOnlyList<IReadOnlyList<string>> _rows = [];
    private int _row = -1;
    private int _key = -1;
    private int _passes;

    public event EventHandler? HighlightChanged;

    public bool IsRunning { get; private set; }

    public ScanPhase Phase { get; private set; } = ScanPhase.Idle;

    /// <summary>How many times to sweep a selected row before falling back to row scanning.</summary>
    public int MaxRowPasses { get; set; } = 2;

    public int HighlightedRow => Phase == ScanPhase.Idle ? -1 : _row;

    /// <summary>Ids of every key in the highlighted row (row phase) or just the highlighted key (key phase).</summary>
    public IReadOnlyList<string> Highlighted => Phase switch
    {
        ScanPhase.Rows when _row >= 0 => _rows[_row],
        ScanPhase.Keys when _row >= 0 && _key >= 0 => [_rows[_row][_key]],
        _ => [],
    };

    public void Start(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _rows = rows.Where(r => r.Count > 0).ToArray();
        IsRunning = _rows.Count > 0;
        Phase = IsRunning ? ScanPhase.Rows : ScanPhase.Idle;
        _row = IsRunning ? 0 : -1;
        _key = -1;
        _passes = 0;
        HighlightChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        IsRunning = false;
        Phase = ScanPhase.Idle;
        _row = -1;
        _key = -1;
        HighlightChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Advance the highlight one step. Called by the host on the scan interval.</summary>
    public void Tick()
    {
        if (!IsRunning)
        {
            return;
        }

        switch (Phase)
        {
            case ScanPhase.Rows:
                _row = (_row + 1) % _rows.Count;
                break;
            case ScanPhase.Keys:
                _key++;
                if (_key >= _rows[_row].Count)
                {
                    _key = 0;
                    _passes++;
                    if (_passes >= MaxRowPasses)
                    {
                        Phase = ScanPhase.Rows;
                        _key = -1;
                        _passes = 0;
                    }
                }

                break;
            default:
                return;
        }

        HighlightChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The switch was pressed. In the row phase this enters the row; in the key phase it returns the
    /// selected key id and resumes row scanning from that row.
    /// </summary>
    public string? Select()
    {
        if (!IsRunning)
        {
            return null;
        }

        switch (Phase)
        {
            case ScanPhase.Rows:
                Phase = ScanPhase.Keys;
                _key = 0;
                _passes = 0;
                HighlightChanged?.Invoke(this, EventArgs.Empty);
                return null;
            case ScanPhase.Keys:
                var selected = _rows[_row][_key];
                Phase = ScanPhase.Rows;
                _key = -1;
                _passes = 0;
                HighlightChanged?.Invoke(this, EventArgs.Empty);
                return selected;
            default:
                return null;
        }
    }
}

public enum ScanPhase
{
    Idle,
    Rows,
    Keys,
}
