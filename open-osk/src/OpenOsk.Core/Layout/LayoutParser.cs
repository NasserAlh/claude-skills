using System.Text.Json;
using System.Text.Json.Serialization;
using OpenOsk.Core.Keys;

namespace OpenOsk.Core.Layout;

/// <summary>
/// Reads the JSON layout format documented in <c>docs/layouts.md</c>. Keys are described by what they do
/// (<c>vk</c>, <c>mod</c>, <c>cmd</c>, <c>gap</c>) and the parser derives everything else.
/// </summary>
public static class LayoutParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static KeyboardLayout Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var doc = JsonSerializer.Deserialize<LayoutDocument>(json, Options)
                  ?? throw new LayoutFormatException("Layout document is empty.");
        if (string.IsNullOrWhiteSpace(doc.Id))
        {
            throw new LayoutFormatException("Layout is missing an \"id\".");
        }

        if (doc.Rows is null || doc.Rows.Count == 0)
        {
            throw new LayoutFormatException($"Layout \"{doc.Id}\" has no rows.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rows = ParseBlock(doc.Rows, "row", seen);
        var numPad = ParseBlock(doc.NumPad ?? [], "numpad", seen);
        var commands = ParseBlock(doc.Commands ?? [], "cmd", seen);
        return new KeyboardLayout(doc.Id, doc.Name ?? doc.Id, rows, numPad, commands);
    }

    private static List<IReadOnlyList<KeyDefinition>> ParseBlock(
        List<List<KeyEntry>> block, string prefix, HashSet<string> seen)
    {
        var rows = new List<IReadOnlyList<KeyDefinition>>(block.Count);
        for (var r = 0; r < block.Count; r++)
        {
            var row = new List<KeyDefinition>(block[r].Count);
            for (var c = 0; c < block[r].Count; c++)
            {
                var key = ParseKey(block[r][c], $"{prefix}{r}-{c}");
                if (!key.IsGap && !seen.Add(key.Id))
                {
                    throw new LayoutFormatException($"Duplicate key id \"{key.Id}\".");
                }

                row.Add(key);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static KeyDefinition ParseKey(KeyEntry entry, string defaultId)
    {
        if (entry.Gap is { } gap)
        {
            if (gap <= 0)
            {
                throw new LayoutFormatException($"Gap width must be positive (key {defaultId}).");
            }

            return KeyDefinition.Gap(gap);
        }

        var width = entry.W ?? 1.0;
        if (width <= 0)
        {
            throw new LayoutFormatException($"Key width must be positive (key {defaultId}).");
        }

        if (entry.Mod is not null)
        {
            var modifier = ParseEnum<ModifierKey>(entry.Mod, "mod", defaultId);
            var modifierVk = entry.Vk is null ? ModifierKeyInfo.ToVirtualKey(modifier) : ParseEnum<VirtualKey>(entry.Vk, "vk", defaultId);
            return new KeyDefinition
            {
                Id = entry.Id ?? $"mod-{modifier}-{defaultId}",
                Kind = KeyKind.Modifier,
                Modifier = modifier,
                VirtualKey = modifierVk,
                Label = entry.Label ?? DefaultModifierLabel(modifier),
                Width = width,
                Extended = entry.Ext ?? VirtualKeyInfo.IsExtended(modifierVk),
                Repeat = false,
            };
        }

        if (entry.Cmd is not null)
        {
            var command = ParseEnum<OskCommand>(entry.Cmd, "cmd", defaultId);
            return new KeyDefinition
            {
                Id = entry.Id ?? $"cmd-{command}",
                Kind = KeyKind.Command,
                Command = command,
                Label = entry.Label ?? DefaultCommandLabel(command),
                Width = width,
                Repeat = false,
            };
        }

        if (entry.Vk is null)
        {
            throw new LayoutFormatException($"Key {defaultId} needs one of \"vk\", \"mod\", \"cmd\" or \"gap\".");
        }

        var vk = ParseEnum<VirtualKey>(entry.Vk, "vk", defaultId);
        var isLock = entry.Lock ?? vk is VirtualKey.Capital or VirtualKey.NumLock or VirtualKey.Scroll;
        var isChar = entry.Char ?? false;
        var kind = isLock ? KeyKind.Lock : isChar ? KeyKind.Character : KeyKind.Action;
        return new KeyDefinition
        {
            Id = entry.Id ?? $"{(isChar ? "chr" : "key")}-{vk}-{defaultId}",
            Kind = kind,
            VirtualKey = vk,
            FnVirtualKey = entry.Fn is null ? VirtualKey.None : ParseEnum<VirtualKey>(entry.Fn, "fn", defaultId),
            Label = entry.Label ?? DefaultLabel(vk),
            ShiftLabel = entry.ShiftLabel,
            Width = width,
            Extended = entry.Ext ?? VirtualKeyInfo.IsExtended(vk),
            Repeat = entry.Repeat ?? !isLock,
        };
    }

    private static T ParseEnum<T>(string value, string field, string keyId)
        where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new LayoutFormatException($"Unknown {field} \"{value}\" on key {keyId}.");
    }

    private static string DefaultModifierLabel(ModifierKey modifier) => modifier switch
    {
        ModifierKey.Shift => "Shift",
        ModifierKey.Control => "Ctrl",
        ModifierKey.Alt => "Alt",
        ModifierKey.Win => "⊞",
        ModifierKey.Fn => "Fn",
        _ => modifier.ToString(),
    };

    private static string DefaultCommandLabel(OskCommand command) => command switch
    {
        OskCommand.Navigation => "Nav",
        OskCommand.General => "Gen",
        OskCommand.MoveUp => "Mv Up",
        OskCommand.MoveDown => "Mv Dn",
        OskCommand.Dock => "Dock",
        OskCommand.Fade => "Fade",
        OskCommand.Options => "Options",
        OskCommand.Help => "Help",
        OskCommand.NumPad => "NumPad",
        _ => command.ToString(),
    };

    private static string DefaultLabel(VirtualKey vk) => vk switch
    {
        VirtualKey.Back => "⌫",
        VirtualKey.Tab => "Tab",
        VirtualKey.Return => "↵",
        VirtualKey.Escape => "Esc",
        VirtualKey.Space => "Space",
        VirtualKey.Capital => "Caps",
        VirtualKey.NumLock => "NumLk",
        VirtualKey.Scroll => "ScrLk",
        VirtualKey.Pause => "Pause",
        VirtualKey.Snapshot => "PrtScn",
        VirtualKey.Insert => "Insert",
        VirtualKey.Delete => "Del",
        VirtualKey.Home => "Home",
        VirtualKey.End => "End",
        VirtualKey.Prior => "PgUp",
        VirtualKey.Next => "PgDn",
        VirtualKey.Left => "←",
        VirtualKey.Up => "↑",
        VirtualKey.Right => "→",
        VirtualKey.Down => "↓",
        VirtualKey.Apps => "☰",
        VirtualKey.Multiply => "*",
        VirtualKey.Add => "+",
        VirtualKey.Subtract => "-",
        VirtualKey.Decimal => ".",
        VirtualKey.Divide => "/",
        _ when VirtualKeyInfo.IsNumPad(vk) => ((int)vk - (int)VirtualKey.NumPad0).ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ when VirtualKeyInfo.IsDigit(vk) => ((char)('0' + (vk - VirtualKey.D0))).ToString(),
        _ when VirtualKeyInfo.IsLetter(vk) => ((char)('a' + (vk - VirtualKey.A))).ToString(),
        _ => vk.ToString(),
    };

    private sealed class LayoutDocument
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public List<List<KeyEntry>>? Rows { get; set; }

        public List<List<KeyEntry>>? NumPad { get; set; }

        public List<List<KeyEntry>>? Commands { get; set; }
    }

    private sealed class KeyEntry
    {
        public string? Id { get; set; }

        public string? Vk { get; set; }

        public string? Fn { get; set; }

        public string? Mod { get; set; }

        public string? Cmd { get; set; }

        public double? Gap { get; set; }

        public string? Label { get; set; }

        [JsonPropertyName("shift")]
        public string? ShiftLabel { get; set; }

        public double? W { get; set; }

        public bool? Char { get; set; }

        public bool? Ext { get; set; }

        public bool? Lock { get; set; }

        public bool? Repeat { get; set; }
    }
}

public sealed class LayoutFormatException : Exception
{
    public LayoutFormatException(string message)
        : base(message)
    {
    }

    public LayoutFormatException()
    {
    }

    public LayoutFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
