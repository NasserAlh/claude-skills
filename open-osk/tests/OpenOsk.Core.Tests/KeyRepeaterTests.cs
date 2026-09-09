using OpenOsk.Core.Input;
using Xunit;

namespace OpenOsk.Core.Tests;

public class KeyRepeaterTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan Interval = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 30);

    private static TimeSpan Ms(double ms) => TimeSpan.FromMilliseconds(ms);

    [Fact]
    public void NothingIsDueBeforeTheDelay()
    {
        var r = new KeyRepeater(Delay, Interval);
        r.Start(TimeSpan.Zero);
        Assert.Equal(0, r.Due(Ms(499)));
        Assert.Equal(1, r.Due(Ms(500)));
        Assert.Equal(0, r.Due(Ms(510)));
        Assert.Equal(1, r.Due(Ms(534)));
    }

    [Fact]
    public void ExactTicksGiveOneRepeatEach()
    {
        var r = new KeyRepeater(Delay, Interval);
        r.Start(Ms(100));
        for (var i = 0; i < 30; i++)
        {
            Assert.Equal(1, r.Due(Ms(100) + Delay + (Interval * i)));
        }
    }

    [Fact]
    public void LateTicksAreCaughtUpSoTheAverageRateHolds()
    {
        // A 33 ms DispatcherTimer really ticks about every 47 ms. Over 1.5 s of repeating at 30/s
        // the target must still see 45 key-downs, never more than two per tick.
        var r = new KeyRepeater(Delay, Interval);
        r.Start(TimeSpan.Zero);
        var total = 0;
        for (var t = 0; t <= 1974; t += 47)
        {
            var due = r.Due(Ms(t));
            Assert.InRange(due, 0, 2);
            total += due;
        }

        Assert.Equal(45, total);
    }

    [Fact]
    public void ALongStallIsCappedAndTheBacklogDropped()
    {
        var r = new KeyRepeater(Delay, Interval);
        r.Start(TimeSpan.Zero);
        Assert.Equal(KeyRepeater.MaxBurst, r.Due(Ms(5000)));
        Assert.Equal(0, r.Due(Ms(5000)));
        Assert.Equal(0, r.Due(Ms(5000) + (Interval / 2)));
        Assert.Equal(1, r.Due(Ms(5000) + Interval));
    }

    [Fact]
    public void StopEndsRepeats()
    {
        var r = new KeyRepeater(Delay, Interval);
        r.Start(TimeSpan.Zero);
        Assert.True(r.IsRunning);
        r.Stop();
        Assert.False(r.IsRunning);
        Assert.Equal(0, r.Due(Ms(10000)));
    }

    [Fact]
    public void RestartResetsTheDelay()
    {
        var r = new KeyRepeater(Delay, Interval);
        r.Start(TimeSpan.Zero);
        Assert.Equal(1, r.Due(Ms(500)));
        r.Start(Ms(600));
        Assert.Equal(0, r.Due(Ms(1000)));
        Assert.Equal(1, r.Due(Ms(1100)));
    }
}
