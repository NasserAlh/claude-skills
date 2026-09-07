# OpenOSK

An open-source on-screen keyboard for Windows 10 and 11 that does what the built-in
On-Screen Keyboard (`osk.exe`) does, without the parts that make it freeze and without sending
anything to Microsoft.

- **No telemetry, no network code, no hooks.** Everything stays on your PC. See [docs/privacy.md](docs/privacy.md).
- **Does not freeze.** The native OSK stalls because it sits inside the system input path (a
  low-level keyboard hook, text services, an accessibility server). OpenOSK uses none of them;
  see [docs/architecture.md](docs/architecture.md#why-it-does-not-freeze).
- **Feature-for-feature with the Windows OSK**: click, hover and scan typing, sticky modifiers,
  Fn row, numeric pad, Nav / Mv Up / Mv Dn / Dock / Fade, text prediction, click sound, start at
  sign-in. The full comparison is in [docs/feature-parity.md](docs/feature-parity.md).
- **Follows your input language.** Key labels come from the layout of the application you are
  typing into, so AZERTY, QWERTZ, Arabic or Greek just work.
- **Yours to change.** MIT licensed, one `dotnet publish` away from your own build, with a core
  library that is unit-tested on every OS.

## Install

Download `OpenOSK-win-x64.exe` (or `-win-arm64`) from the Releases page and run it. It is a single
self-contained file; nothing else is installed and there is no runtime to download. To start it
with Windows, tick *Start OpenOSK when I sign in* in Options.

Verify the download with the `.sha256` file published beside it.

## Use

| Action | How |
|---|---|
| Type | Click a key. Hold it to repeat. |
| Shift, Ctrl, Alt, Win, Fn | Tap once to apply to the next key; tap twice to lock; tap again to release. |
| F1–F12 | Tap **Fn**; the number row changes. |
| Word suggestions | Click a word above the keys to complete what you are typing. |
| Move | Drag the title bar, or use **Mv Up** / **Mv Dn**. |
| Dock | **Dock** pins the keyboard to the bottom edge and other windows make room. Tap again to undock. |
| See through it | **Fade** makes it translucent until you point at it. |
| Navigation keys only | **Nav**; **Gen** brings the full keyboard back. |
| Hover to type | Options → *Hover over keys*, set the duration. |
| Scan with one switch | Options → *Scan through keys*. Space (or a key you choose) or a click selects. |
| Numeric pad | Options → *Turn on numeric key pad*. |
| Theme | Options → *Appearance*. Follows Windows light/dark by default. |

Command line: `OpenOSK.exe --nav` starts in navigation mode, `OpenOSK.exe --dock` starts docked.
Launching it a second time brings the existing keyboard to the front.

## Build from source

Requires the .NET 8 SDK. Windows is only needed to *run* the keyboard; the solution compiles and
the tests pass on Linux and macOS.

```powershell
dotnet test open-osk/tests/OpenOsk.Core.Tests; dotnet publish open-osk/src/OpenOsk/OpenOsk.csproj -c Release -r win-x64 -o out
```

`out\OpenOSK.exe` is the finished keyboard. See [CONTRIBUTING.md](CONTRIBUTING.md) for the
project rules and [docs/layouts.md](docs/layouts.md) to add a layout.

## Limitations

Windows will not deliver injected keys to an application running as administrator unless OpenOSK
also runs as administrator. The sign-in screen and UAC prompts can only use Microsoft's keyboard.
32-bit Windows is not supported.

## Licence

MIT. See [LICENSE](../LICENSE).
