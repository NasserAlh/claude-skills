using OpenOsk.Core.Keys;
using OpenOsk.Core.Settings;
using OpenOsk.Core.Typing;
using Xunit;

namespace OpenOsk.Core.Tests;

public class SettingsTests
{
    [Fact]
    public void RoundTripsThroughJson()
    {
        var s = new OskSettings
        {
            TypingMode = TypingMode.Scan,
            ScanSeconds = 2.5,
            ScanSelectKey = VirtualKey.F12,
            ShowNumPad = true,
            Theme = OskTheme.Dark,
            Window = new WindowPlacement(10, 20, 800, 300),
        };
        var json = SettingsStore.Serialize(s);
        Assert.Contains("\"typingMode\": \"Scan\"", json, StringComparison.Ordinal);
        var back = SettingsStore.Deserialize(json);
        Assert.Equal(TypingMode.Scan, back.TypingMode);
        Assert.Equal(2.5, back.ScanSeconds);
        Assert.Equal(VirtualKey.F12, back.ScanSelectKey);
        Assert.True(back.ShowNumPad);
        Assert.Equal(OskTheme.Dark, back.Theme);
        Assert.Equal(new WindowPlacement(10, 20, 800, 300), back.Window);
    }

    [Fact]
    public void ValuesAreClamped()
    {
        var s = new OskSettings { HoverSeconds = 99, ScanSeconds = 0, FadeOpacity = 0, PredictionCount = 50 }.Normalized();
        Assert.Equal(3.0, s.HoverSeconds);
        Assert.Equal(0.5, s.ScanSeconds);
        Assert.Equal(0.1, s.FadeOpacity);
        Assert.Equal(10, s.PredictionCount);
    }

    [Fact]
    public void UnknownEnumValuesFallBackToDefaults()
    {
        var s = SettingsStore.Deserialize("""{ "typingMode": "Click", "scanSelectKey": "None", "layout": " " }""");
        Assert.Equal(VirtualKey.Space, s.ScanSelectKey);
        Assert.Equal("standard", s.Layout);
    }

    [Fact]
    public void MissingOrCorruptFileYieldsDefaults()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"openosk-test-{Guid.NewGuid():N}");
        var path = Path.Combine(dir, "settings.json");
        try
        {
            var store = new SettingsStore(path);
            Assert.Equal(TypingMode.Click, store.Load().TypingMode);
            store.Save(new OskSettings { TypingMode = TypingMode.Hover });
            Assert.Equal(TypingMode.Hover, store.Load().TypingMode);
            File.WriteAllText(path, "{ this is not json");
            Assert.Equal(TypingMode.Click, store.Load().TypingMode);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
