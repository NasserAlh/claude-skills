# Contributing to OpenOSK

Thanks for helping. A few ground rules keep the project honest about its promises.

## Non-negotiables

1. **No network code.** Not for updates, not for crash reports, not "opt-in". A pull request that
   adds `HttpClient`, sockets or any SDK that phones home will be closed.
2. **No keyboard hooks.** `SetWindowsHookEx` is how the native OSK freezes the machine. If a
   feature seems to need one, open an issue first; there is usually a polling or `RegisterHotKey`
   alternative.
3. **Core stays platform-independent.** Anything in `src/OpenOsk.Core` must compile and test on
   Linux. Win32 lives in `src/OpenOsk/Native` only.
4. **Logic goes in Core with a test.** If a change to stroke ordering, modifiers, scanning,
   prediction or settings can be unit-tested, it must be.

## Building

```powershell
dotnet build open-osk/OpenOsk.sln -c Release
dotnet test open-osk/tests/OpenOsk.Core.Tests
dotnet publish open-osk/src/OpenOsk/OpenOsk.csproj -c Release -r win-x64 -o out
```

The WPF project builds on Linux and macOS too (`EnableWindowsTargeting` is set) so the whole
solution can be compiled and the core tested without a Windows machine. Running the app needs
Windows 10 1809 or later.

Warnings are errors. The `.editorconfig` defines formatting; `dotnet format` will
apply it.

## Pull requests

- One change per PR, with a short description of *why*.
- Keep `docs/feature-parity.md` truthful: if you add or change behaviour, update the table.
- Add a line to `CHANGELOG.md` under *Unreleased*.

## Reporting a freeze or a lost key

Please include: Windows version, the application you were typing into, whether it runs as
administrator, the typing mode (click / hover / scan), and the contents of
`%LOCALAPPDATA%\OpenOSK\error.log` if present. Never paste `learned-words.txt`.
