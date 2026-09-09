using OpenOsk.Core.Input;
using OpenOsk.Core.Keys;
using OpenOsk.Core.Layout;
using OpenOsk.Core.Modifiers;
using Xunit;

namespace OpenOsk.Core.Tests;

public class KeyStrokePlannerTests
{
    private static KeyDefinition Key(VirtualKey vk) =>
        BuiltInLayouts.Standard.AllKeys().First(k => k.VirtualKey == vk && k.Kind != KeyKind.Modifier);

    [Fact]
    public void PlainKeyIsDownThenUp()
    {
        var plan = KeyStrokePlanner.Plan(Key(VirtualKey.A), new ModifierController());
        Assert.Equal([KeyStroke.Down(VirtualKey.A)], plan.Press);
        Assert.Equal([KeyStroke.Up(VirtualKey.A)], plan.Release);
        Assert.Equal([KeyStroke.Down(VirtualKey.A)], plan.Repeat);
        Assert.True(plan.ConsumesLatchedModifiers);
    }

    [Fact]
    public void ModifiersWrapTheKeyInOrder()
    {
        var mods = new ModifierController();
        mods.Tap(ModifierKey.Control);
        mods.Tap(ModifierKey.Shift);
        var plan = KeyStrokePlanner.Plan(Key(VirtualKey.A), mods);
        Assert.Equal(
            [KeyStroke.Down(VirtualKey.LShift), KeyStroke.Down(VirtualKey.LControl), KeyStroke.Down(VirtualKey.A)],
            plan.Press);
        Assert.Equal(
            [KeyStroke.Up(VirtualKey.A), KeyStroke.Up(VirtualKey.LControl), KeyStroke.Up(VirtualKey.LShift)],
            plan.Release);
    }

    [Fact]
    public void FnRemapsNumberRowAndIsNeverSent()
    {
        var mods = new ModifierController();
        mods.Tap(ModifierKey.Fn);
        var plan = KeyStrokePlanner.Plan(Key(VirtualKey.D1), mods);
        Assert.Equal([KeyStroke.Down(VirtualKey.F1)], plan.Press);
        Assert.Equal([KeyStroke.Up(VirtualKey.F1)], plan.Release);
    }

    [Fact]
    public void FnLeavesUnmappedKeysAlone()
    {
        var mods = new ModifierController();
        mods.Tap(ModifierKey.Fn);
        var plan = KeyStrokePlanner.Plan(Key(VirtualKey.A), mods);
        Assert.Equal([KeyStroke.Down(VirtualKey.A)], plan.Press);
    }

    [Fact]
    public void NumPadKeysBecomeNavigationKeysWhileNumLockIsOff()
    {
        var seven = Key(VirtualKey.NumPad7);
        Assert.Equal([KeyStroke.Down(VirtualKey.NumPad7)], KeyStrokePlanner.Plan(seven, new ModifierController(), numLock: true).Press);

        var plan = KeyStrokePlanner.Plan(seven, new ModifierController(), numLock: false);
        Assert.Equal([KeyStroke.Down(VirtualKey.Home)], plan.Press);
        Assert.Equal([KeyStroke.Up(VirtualKey.Home)], plan.Release);
        Assert.Equal([KeyStroke.Down(VirtualKey.Home)], plan.Repeat);
        Assert.Equal([KeyStroke.Down(VirtualKey.Delete)], KeyStrokePlanner.Plan(Key(VirtualKey.Decimal), new ModifierController(), numLock: false).Press);
    }

    [Fact]
    public void NumLockDoesNotAffectOtherKeys()
    {
        Assert.Equal([KeyStroke.Down(VirtualKey.A)], KeyStrokePlanner.Plan(Key(VirtualKey.A), new ModifierController(), numLock: false).Press);
        Assert.Equal([KeyStroke.Down(VirtualKey.Home, extended: true)], KeyStrokePlanner.Plan(Key(VirtualKey.Home), new ModifierController(), numLock: false).Press);
        Assert.Equal([KeyStroke.Down(VirtualKey.Add)], KeyStrokePlanner.Plan(Key(VirtualKey.Add), new ModifierController(), numLock: false).Press);
    }

    [Fact]
    public void ExtendedKeysCarryTheFlag()
    {
        var plan = KeyStrokePlanner.Plan(Key(VirtualKey.Left), new ModifierController());
        Assert.Equal([KeyStroke.Down(VirtualKey.Left, extended: true)], plan.Press);
    }

    [Fact]
    public void WinModifierIsExtended()
    {
        var mods = new ModifierController();
        mods.Tap(ModifierKey.Win);
        var plan = KeyStrokePlanner.Plan(Key(VirtualKey.D), mods);
        Assert.Equal(KeyStroke.Down(VirtualKey.LWin, extended: true), plan.Press[0]);
    }

    [Fact]
    public void LockKeysDoNotRepeat()
    {
        var plan = KeyStrokePlanner.Plan(Key(VirtualKey.Capital), new ModifierController());
        Assert.Empty(plan.Repeat);
        Assert.Equal([KeyStroke.Down(VirtualKey.Capital)], plan.Press);
    }

    [Fact]
    public void CommandsAndModifiersProduceNothing()
    {
        var cmd = BuiltInLayouts.Standard.AllKeys().First(k => k.Kind == KeyKind.Command);
        var mod = BuiltInLayouts.Standard.AllKeys().First(k => k.Kind == KeyKind.Modifier);
        Assert.Same(KeyStrokePlan.Empty, KeyStrokePlanner.Plan(cmd, new ModifierController()));
        Assert.Same(KeyStrokePlan.Empty, KeyStrokePlanner.Plan(mod, new ModifierController()));
    }

    [Fact]
    public void TapIsPressFollowedByRelease()
    {
        var mods = new ModifierController();
        mods.Tap(ModifierKey.Shift);
        var plan = KeyStrokePlanner.Plan(Key(VirtualKey.Tab), mods);
        Assert.Equal([.. plan.Press, .. plan.Release], plan.Tap);
    }
}
