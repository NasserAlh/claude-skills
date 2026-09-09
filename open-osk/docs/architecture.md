# Architecture

OpenOSK is a WPF application on .NET 8 with a deliberately thin Windows layer over a
platform-independent core.

```
OpenOSK/
├── src/OpenOsk.Core/        net8.0 class library, no Windows dependency, fully unit-tested
│   ├── Keys/                VirtualKey codes, KeyDefinition, KeyKind, OskCommand, ModifierKey
│   ├── Layout/              KeyboardLayout model, JSON LayoutParser, BuiltInLayouts
│   ├── Layouts/*.json       Embedded layouts (standard, navigation)
│   ├── Modifiers/           ModifierController: the sticky Shift/Ctrl/Alt/Win/Fn state machine
│   ├── Input/               KeyStroke, KeyStrokePlanner (key + modifiers -> strokes), IKeyInjector
│   ├── Typing/              DwellTracker (hover mode), ScanController (scan mode)
│   ├── Prediction/          WordPredictor, TypedWordTracker, lexicon and learned-word stores
│   └── Settings/            OskSettings and the JSON SettingsStore
├── src/OpenOsk/             net8.0-windows WPF host
│   ├── Native/              Every P/Invoke, window-style helpers, the AppBar (Dock) implementation
│   ├── Input/               SendInputInjector, KeyboardStateMonitor (polling), GlobalHotKey
│   ├── Layout/              KeyLabelProvider: labels from the active input language via ToUnicodeEx
│   ├── Services/            Click sound, theme, single instance, startup registration, paths
│   ├── Controls/KeyButton   One key: state exposed as dependency properties, no input handling
│   ├── Themes/              Light, Dark, HighContrast palettes and the control templates
│   ├── MainWindow           The keyboard window
│   ├── OptionsWindow        The options dialog
│   └── HelpWindow
└── tests/OpenOsk.Core.Tests xunit tests for the core (run on Linux, macOS and Windows)
```

## Why it does not freeze

The native Windows OSK's stalls come from work done *inside the input path*: a low-level keyboard
hook, text-services (TSF) integration and a UI Automation server all run synchronously while the
user is typing, and any one of them blocking stalls the keyboard for every application.

OpenOSK avoids that whole class of problem:

| Concern | Windows OSK | OpenOSK |
|---|---|---|
| Seeing lock/modifier state | Low-level keyboard hook | `GetKeyState` / `GetAsyncKeyState` polled every 100 ms on the UI thread |
| Sending keys | `SendInput` | `SendInput`, one call per batch, never blocking |
| Keeping the target app focused | `WS_EX_NOACTIVATE` | `WS_EX_NOACTIVATE` plus `WM_MOUSEACTIVATE` returning `MA_NOACTIVATE` |
| Key labels per language | Text services | `ToUnicodeEx` with the "do not change keyboard state" flag, cached |
| Scan-mode switch key | Keyboard hook | `RegisterHotKey`, only while scan mode is on |
| Prediction | Cloud / system dictionary | In-process trie over an embedded word list plus a local learned list |

Nothing in the program can block on another process. Every timer runs on the WPF dispatcher and
does a bounded amount of work; every file write is atomic (write to `.tmp`, then rename).

## Data flow for one key press

1. `MainWindow` receives the pointer event on a `KeyButton` (click mode), or `DwellTracker` fires
   (hover mode), or `ScanController.Select()` returns a key id (scan mode).
2. `KeyStrokePlanner.Plan(key, modifiers)` produces the press and release stroke lists. Active
   sticky modifiers wrap the key; Fn remaps the number row to F-keys.
3. `TypedWordTracker` is updated from the label the key currently shows, so prediction follows
   the same layout the user sees.
4. `SendInputInjector.Send(plan.Press)` delivers the batch; the OS routes it to the foreground
   window, which is still the user's application because OpenOSK never activates.
5. On release, `plan.Release` is sent and latched modifiers are consumed. Lock keys trigger an
   immediate re-poll so the Caps/Num/Scroll indicator updates.

## Layout files

Layouts are JSON. The format is documented in [layouts.md](layouts.md). Built-in layouts are
embedded; a user can drop additional files in `%LOCALAPPDATA%\OpenOSK\layouts` and pick them
in Options. Labels of `char` keys come from the OS, so one layout file serves every language
that shares the physical key arrangement.

## Testing strategy

The core project holds every piece of logic that can be wrong in a subtle way (stroke ordering,
modifier consumption, dwell timing, scan fall-back, prediction ranking, settings clamping) and has
no Windows dependency, so the tests run on any OS and in CI on both Ubuntu and Windows. The WPF
host is kept to wiring and rendering, and is compiled on Linux in CI through
`EnableWindowsTargeting` to catch build breaks early even without a Windows runner.
