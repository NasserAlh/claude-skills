using OpenOsk.Core.Keys;
using OpenOsk.Core.Layout;
using Xunit;

namespace OpenOsk.Core.Tests;

public class LayoutTests
{
    [Fact]
    public void BuiltInLayoutsLoad()
    {
        Assert.Contains(BuiltInLayouts.StandardId, BuiltInLayouts.All.Keys);
        Assert.Contains(BuiltInLayouts.NavigationId, BuiltInLayouts.All.Keys);
        Assert.Equal(5, BuiltInLayouts.Standard.Rows.Count);
        Assert.Equal(5, BuiltInLayouts.Standard.NumPadRows.Count);
        Assert.Equal(5, BuiltInLayouts.Standard.CommandRows.Count);
    }

    [Fact]
    public void StandardLayoutHasEveryLetterAndDigit()
    {
        var keys = BuiltInLayouts.Standard.AllKeys().ToList();
        for (var vk = VirtualKey.A; vk <= VirtualKey.Z; vk++)
        {
            Assert.Contains(keys, k => k.VirtualKey == vk && k.Kind == KeyKind.Character);
        }

        for (var vk = VirtualKey.D0; vk <= VirtualKey.D9; vk++)
        {
            Assert.Contains(keys, k => k.VirtualKey == vk && k.Kind == KeyKind.Character);
        }
    }

    [Fact]
    public void NumberRowMapsToFunctionKeysUnderFn()
    {
        var one = BuiltInLayouts.Standard.AllKeys().Single(k => k.VirtualKey == VirtualKey.D1);
        Assert.Equal(VirtualKey.F1, one.FnVirtualKey);
        var zero = BuiltInLayouts.Standard.AllKeys().Single(k => k.VirtualKey == VirtualKey.D0);
        Assert.Equal(VirtualKey.F10, zero.FnVirtualKey);
    }

    [Fact]
    public void MainRowsAreEqualWidth()
    {
        var widths = BuiltInLayouts.Standard.Rows.Select(r => r.Sum(k => k.Width)).ToList();
        Assert.All(widths, w => Assert.Equal(widths[0], w, precision: 6));
    }

    [Fact]
    public void ExtendedFlagIsDerivedForNavigationKeys()
    {
        var keys = BuiltInLayouts.Standard.AllKeys().ToList();
        Assert.True(keys.Single(k => k.VirtualKey == VirtualKey.Left).Extended);
        Assert.True(keys.Single(k => k.VirtualKey == VirtualKey.Delete).Extended);
        Assert.True(keys.Single(k => k.VirtualKey == VirtualKey.RControl).Extended);
        Assert.False(keys.Single(k => k.VirtualKey == VirtualKey.LControl).Extended);
        Assert.True(keys.Single(k => k.Id == "numpad-enter").Extended);
        Assert.False(keys.Single(k => k.VirtualKey == VirtualKey.Return && k.Id != "numpad-enter").Extended);
    }

    [Fact]
    public void KeyIdsAreUnique()
    {
        foreach (var layout in BuiltInLayouts.All.Values)
        {
            var ids = layout.AllKeys().Select(k => k.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Fact]
    public void ParserDerivesKinds()
    {
        const string json = """
        {
          "id": "t", "name": "Test",
          "rows": [[
            { "vk": "A", "char": true },
            { "vk": "Return" },
            { "vk": "Capital" },
            { "mod": "Shift" },
            { "cmd": "Fade" },
            { "gap": 0.5 }
          ]]
        }
        """;
        var layout = LayoutParser.Parse(json);
        var row = layout.Rows[0];
        Assert.Equal(KeyKind.Character, row[0].Kind);
        Assert.Equal(KeyKind.Action, row[1].Kind);
        Assert.Equal(KeyKind.Lock, row[2].Kind);
        Assert.False(row[2].Repeat);
        Assert.Equal(KeyKind.Modifier, row[3].Kind);
        Assert.Equal(VirtualKey.LShift, row[3].VirtualKey);
        Assert.Equal(KeyKind.Command, row[4].Kind);
        Assert.Equal(OskCommand.Fade, row[4].Command);
        Assert.True(row[5].IsGap);
        Assert.Equal(5.5, layout.MainWidthUnits, precision: 6);
    }

    [Theory]
    [InlineData("""{ "name": "x", "rows": [[{ "vk": "A" }]] }""")]
    [InlineData("""{ "id": "x", "rows": [] }""")]
    [InlineData("""{ "id": "x", "rows": [[{ "vk": "NotAKey" }]] }""")]
    [InlineData("""{ "id": "x", "rows": [[{ "label": "nothing" }]] }""")]
    [InlineData("""{ "id": "x", "rows": [[{ "id": "a", "vk": "A" }, { "id": "a", "vk": "B" }]] }""")]
    [InlineData("""{ "id": "x", "rows": [[{ "vk": "A", "w": 0 }]] }""")]
    public void ParserRejectsBadDocuments(string json)
    {
        Assert.Throws<LayoutFormatException>(() => LayoutParser.Parse(json));
    }

    [Fact]
    public void ScanRowsIncludeCommandsAndOptionalNumPad()
    {
        var layout = BuiltInLayouts.Standard;
        var without = layout.ScanRows(includeNumPad: false);
        var with = layout.ScanRows(includeNumPad: true);
        Assert.Equal(5, without.Count);
        Assert.Contains("cmd-Navigation", without[0]);
        Assert.DoesNotContain(without[0], id => id.Contains("NumLock", StringComparison.Ordinal));
        Assert.Contains(with[0], id => id.Contains("NumLock", StringComparison.Ordinal));
        Assert.All(without, row => Assert.DoesNotContain(row, id => id.StartsWith("gap-", StringComparison.Ordinal)));
    }
}
