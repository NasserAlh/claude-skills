using OpenOsk.Core.Keys;
using OpenOsk.Core.Modifiers;
using Xunit;

namespace OpenOsk.Core.Tests;

public class ModifierControllerTests
{
    [Fact]
    public void TapCyclesThroughLatchedAndLocked()
    {
        var m = new ModifierController();
        Assert.Equal(LatchState.Released, m[ModifierKey.Shift]);
        m.Tap(ModifierKey.Shift);
        Assert.Equal(LatchState.Latched, m[ModifierKey.Shift]);
        m.Tap(ModifierKey.Shift);
        Assert.Equal(LatchState.Locked, m[ModifierKey.Shift]);
        m.Tap(ModifierKey.Shift);
        Assert.Equal(LatchState.Released, m[ModifierKey.Shift]);
    }

    [Fact]
    public void SecondTapReleasesWhenLockingDisabled()
    {
        var m = new ModifierController { AllowLock = false };
        m.Tap(ModifierKey.Control);
        m.Tap(ModifierKey.Control);
        Assert.Equal(LatchState.Released, m[ModifierKey.Control]);
    }

    [Fact]
    public void ConsumeLatchedKeepsLocked()
    {
        var m = new ModifierController();
        m.Tap(ModifierKey.Shift);
        m.Tap(ModifierKey.Control);
        m.Tap(ModifierKey.Control);
        m.ConsumeLatched();
        Assert.Equal(LatchState.Released, m[ModifierKey.Shift]);
        Assert.Equal(LatchState.Locked, m[ModifierKey.Control]);
        Assert.Equal([ModifierKey.Control], m.Active);
    }

    [Fact]
    public void ChangedFiresOnlyOnRealChanges()
    {
        var m = new ModifierController();
        var count = 0;
        m.Changed += (_, _) => count++;
        m.Tap(ModifierKey.Alt);
        m.ConsumeLatched();
        m.ConsumeLatched();
        m.ReleaseAll();
        Assert.Equal(2, count);
    }
}
