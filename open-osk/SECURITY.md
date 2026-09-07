# Security

OpenOSK injects keystrokes into whatever application is in the foreground. That is its purpose,
and it is also why the code that decides *what* to send is kept small and fully tested.

## Reporting a vulnerability

Open a GitHub issue with the label `security`, or if the problem could be abused before a fix is
published, email the repository owner listed on the GitHub profile instead. Please include steps
to reproduce. You will get an acknowledgement within a week.

## Scope

In scope:

- Any way for another process, a layout file or a settings file to make OpenOSK send keys the
  user did not press.
- Any way for the program to leak typed text (to disk beyond the documented files, or anywhere
  else).
- Privilege escalation through the app bar, hotkey or startup registration.

Out of scope:

- Windows itself refusing to deliver input to elevated windows (that is the intended UIPI
  behaviour).
- Issues that require the attacker to already run code as the same user.

## Design notes for reviewers

- The only Win32 entry points are listed in `src/OpenOsk/Native/NativeMethods.cs`.
- `SendInput` batches are built exclusively by `KeyStrokePlanner` from a key the user activated.
- Layout files are parsed with `System.Text.Json` into plain records; there is no scripting.
- Settings, learned words and layouts are read from `%LOCALAPPDATA%\OpenOSK` only.
