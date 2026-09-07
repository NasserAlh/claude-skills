# Privacy

OpenOSK is built on one rule: **nothing leaves the machine.**

## What the program does not do

- It makes **no network connections**. There is no update check, no crash reporter, no usage
  statistics, no "improve the product" prompt. The code contains no HTTP client and no socket.
- It installs **no keyboard hook**. It never sees what you type on a physical keyboard, only the
  keys you press on its own window.
- It does **not read other applications' text**. Word prediction works only from the characters
  OpenOSK itself has sent since the last space.
- It does **not run as a service** or stay resident after you close it.

## What it stores, and where

Everything lives in `%LOCALAPPDATA%\OpenOSK` as plain text you can open, edit or delete:

| File | Contents |
|---|---|
| `settings.json` | Options and window position. |
| `learned-words.txt` | Words you typed on OpenOSK, if "Learn the words I type" is on. One word and a count per line. |
| `error.log` | Written only if the program hits an unexpected error. Contains a stack trace, never typed text. |
| `layouts\*.json` | Custom layouts you add yourself. |

"Forget learned words" in Options clears `learned-words.txt`. Deleting the folder resets everything.

The only other change OpenOSK can make outside its own folder is the per-user Run key,
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\OpenOSK`, and only when you tick
"Start OpenOSK when I sign in".

## Building it yourself

The .NET SDK collects build telemetry by default. This repository sets
`DOTNET_CLI_TELEMETRY_OPTOUT=1` in the CI workflow and in `Directory.Build.props`. If you build
locally, set the same environment variable in your shell once:

```powershell
[Environment]::SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1", "User")
```

That opt-out concerns the *build tool*; the published `OpenOSK.exe` contains no telemetry code
regardless of the setting.

## Verifying the claim

Search the source for `HttpClient`, `WebRequest`, `Socket`, `SetWindowsHookEx` or `GetWindowText`:
there are none. The complete list of Win32 functions the program calls is in
`src/OpenOsk/Native/NativeMethods.cs`.
