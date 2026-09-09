using OpenOsk.Core.Typing;
using Xunit;

namespace OpenOsk.Core.Tests;

public class DwellTrackerTests
{
    [Fact]
    public void FiresOnceAfterDwellTime()
    {
        var t = new DwellTracker(TimeSpan.FromSeconds(1));
        t.Enter("a", TimeSpan.Zero);
        Assert.Null(t.Tick(TimeSpan.FromMilliseconds(999)));
        Assert.Equal(0.999, t.Progress(TimeSpan.FromMilliseconds(999)), precision: 3);
        Assert.Equal("a", t.Tick(TimeSpan.FromSeconds(1)));
        Assert.Null(t.Tick(TimeSpan.FromSeconds(5)));
        Assert.Equal(1, t.Progress(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void LeavingCancels()
    {
        var t = new DwellTracker(TimeSpan.FromSeconds(1));
        t.Enter("a", TimeSpan.Zero);
        t.Leave();
        Assert.Null(t.Tick(TimeSpan.FromSeconds(2)));
        Assert.Equal(0, t.Progress(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void MovingToAnotherKeyRestartsTheTimer()
    {
        var t = new DwellTracker(TimeSpan.FromSeconds(1));
        t.Enter("a", TimeSpan.Zero);
        t.Enter("b", TimeSpan.FromMilliseconds(900));
        Assert.Null(t.Tick(TimeSpan.FromMilliseconds(1500)));
        Assert.Equal("b", t.Tick(TimeSpan.FromMilliseconds(1900)));
    }

    [Fact]
    public void ReEnteringSameKeyDoesNotRestart()
    {
        var t = new DwellTracker(TimeSpan.FromSeconds(1));
        t.Enter("a", TimeSpan.Zero);
        t.Enter("a", TimeSpan.FromMilliseconds(900));
        Assert.Equal("a", t.Tick(TimeSpan.FromSeconds(1)));
    }
}

public class ScanControllerTests
{
    private static readonly IReadOnlyList<IReadOnlyList<string>> Rows =
    [
        ["a", "b", "c"],
        ["d", "e"],
        [],
        ["f"],
    ];

    [Fact]
    public void RowsAreHighlightedInTurnAndWrap()
    {
        var s = new ScanController();
        s.Start(Rows);
        Assert.Equal(ScanPhase.Rows, s.Phase);
        Assert.Equal(["a", "b", "c"], s.Highlighted);
        s.Tick();
        Assert.Equal(["d", "e"], s.Highlighted);
        s.Tick();
        Assert.Equal(["f"], s.Highlighted);
        s.Tick();
        Assert.Equal(["a", "b", "c"], s.Highlighted);
    }

    [Fact]
    public void SelectEntersRowThenSelectsKey()
    {
        var s = new ScanController();
        s.Start(Rows);
        s.Tick();
        Assert.Null(s.Select());
        Assert.Equal(ScanPhase.Keys, s.Phase);
        Assert.Equal(["d"], s.Highlighted);
        s.Tick();
        Assert.Equal(["e"], s.Highlighted);
        Assert.Equal("e", s.Select());
        Assert.Equal(ScanPhase.Rows, s.Phase);
        Assert.Equal(1, s.HighlightedRow);
    }

    [Fact]
    public void RowFallsBackAfterMaxPasses()
    {
        var s = new ScanController { MaxRowPasses = 2 };
        s.Start(Rows);
        s.Select();
        for (var i = 0; i < 5; i++)
        {
            s.Tick();
            Assert.Equal(ScanPhase.Keys, s.Phase);
        }

        s.Tick();
        Assert.Equal(ScanPhase.Rows, s.Phase);
        Assert.Equal(["a", "b", "c"], s.Highlighted);
    }

    [Fact]
    public void StopClearsEverything()
    {
        var s = new ScanController();
        s.Start(Rows);
        s.Stop();
        Assert.False(s.IsRunning);
        Assert.Empty(s.Highlighted);
        Assert.Null(s.Select());
        s.Tick();
        Assert.Equal(ScanPhase.Idle, s.Phase);
    }

    [Fact]
    public void EmptyLayoutDoesNotRun()
    {
        var s = new ScanController();
        s.Start([[]]);
        Assert.False(s.IsRunning);
    }
}
