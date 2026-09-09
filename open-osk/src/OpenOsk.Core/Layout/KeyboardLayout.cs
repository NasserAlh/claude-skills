using OpenOsk.Core.Keys;

namespace OpenOsk.Core.Layout;

/// <summary>A complete keyboard: the main key rows, an optional numeric pad and the command column.</summary>
public sealed class KeyboardLayout
{
    public KeyboardLayout(
        string id,
        string name,
        IReadOnlyList<IReadOnlyList<KeyDefinition>> rows,
        IReadOnlyList<IReadOnlyList<KeyDefinition>> numPadRows,
        IReadOnlyList<IReadOnlyList<KeyDefinition>> commandRows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(numPadRows);
        ArgumentNullException.ThrowIfNull(commandRows);
        if (rows.Count == 0)
        {
            throw new ArgumentException("A layout needs at least one row.", nameof(rows));
        }

        Id = id;
        Name = name;
        Rows = rows;
        NumPadRows = numPadRows;
        CommandRows = commandRows;
    }

    public string Id { get; }

    public string Name { get; }

    public IReadOnlyList<IReadOnlyList<KeyDefinition>> Rows { get; }

    public IReadOnlyList<IReadOnlyList<KeyDefinition>> NumPadRows { get; }

    /// <summary>Command keys aligned to the right of the main rows, one entry per main row.</summary>
    public IReadOnlyList<IReadOnlyList<KeyDefinition>> CommandRows { get; }

    public double MainWidthUnits => WidthOf(Rows);

    public double NumPadWidthUnits => WidthOf(NumPadRows);

    public double CommandWidthUnits => WidthOf(CommandRows);

    /// <summary>Every non-gap key across all blocks, in reading order.</summary>
    public IEnumerable<KeyDefinition> AllKeys()
    {
        foreach (var block in new[] { Rows, NumPadRows, CommandRows })
        {
            foreach (var row in block)
            {
                foreach (var key in row)
                {
                    if (!key.IsGap)
                    {
                        yield return key;
                    }
                }
            }
        }
    }

    public KeyDefinition? FindKey(string id) => AllKeys().FirstOrDefault(k => k.Id == id);

    /// <summary>
    /// Rows as the scan mode sees them: main row, then the same-index numpad row, then the same-index
    /// command row, so a single "row" highlight sweeps the whole width of the keyboard.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> ScanRows(bool includeNumPad)
    {
        var result = new List<IReadOnlyList<string>>();
        for (var i = 0; i < Rows.Count; i++)
        {
            var ids = new List<string>();
            ids.AddRange(Rows[i].Where(k => !k.IsGap).Select(k => k.Id));
            if (includeNumPad && i < NumPadRows.Count)
            {
                ids.AddRange(NumPadRows[i].Where(k => !k.IsGap).Select(k => k.Id));
            }

            if (i < CommandRows.Count)
            {
                ids.AddRange(CommandRows[i].Where(k => !k.IsGap).Select(k => k.Id));
            }

            if (ids.Count > 0)
            {
                result.Add(ids);
            }
        }

        return result;
    }

    private static double WidthOf(IReadOnlyList<IReadOnlyList<KeyDefinition>> rows) =>
        rows.Count == 0 ? 0 : rows.Max(r => r.Sum(k => k.Width));
}
