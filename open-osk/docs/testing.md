# Manual test plan

OpenOSK's logic is unit-tested, but the Windows-only behaviour (focus, injection, docking,
fading, hotkeys) can only be checked by running the keyboard. Work through this list on a
Windows 10 or 11 machine after each change to the WPF host. Tick items as you go; anything that
fails goes in an issue with the step number and `%LOCALAPPDATA%\OpenOSK\error.log` if present.

## 0. Build and start

```powershell
dotnet test tests\OpenOsk.Core.Tests; dotnet publish src\OpenOsk\OpenOsk.csproj -c Release -r win-x64 -o out; .\out\OpenOSK.exe
```

- [ ] 0.1 Tests pass. Publish produces a single `out\OpenOSK.exe` and nothing else.
- [ ] 0.2 The keyboard appears near the bottom-centre of the screen, on top, and appears in the taskbar.
- [ ] 0.3 Starting `OpenOSK.exe` a second time does **not** open a second keyboard; the first one comes to the front.

## 1. Focus and typing (the core promise)

Open Notepad and click in it so the caret blinks.

- [ ] 1.1 Click letters on OpenOSK. They appear in Notepad and the caret keeps blinking; Notepad's title bar stays active (not greyed).
- [ ] 1.2 Click the OpenOSK title bar and drag the window. Notepad still stays the active window.
- [ ] 1.3 Hold a letter key down for two seconds: it repeats at the same speed as a physical key.
- [ ] 1.4 Tap **Shift**, then `a`: Notepad shows `A`, and Shift is no longer highlighted.
- [ ] 1.5 Tap **Shift** twice (locked, solid highlight), type `abc`: Notepad shows `ABC`. Tap Shift again to release.
- [ ] 1.6 Tap **Ctrl**, then `a`: everything in Notepad is selected. Then tap **Ctrl**, then `c`; click elsewhere; tap **Ctrl**, `v`: the text is pasted.
- [ ] 1.7 Tap **Fn**: Fn highlights and the number row shows F1–F12. Tap `F5` (was `5`): Notepad inserts the time and date, and the row returns to numbers because Fn is one-shot like Shift. Tap **Fn** twice to lock it (solid highlight, the row stays F1–F12) and once more to release.
- [ ] 1.8 Tap **Caps**: the Caps key turns solid and the letter labels become uppercase. Press Caps Lock on the physical keyboard: the on-screen indicator follows within a moment.
- [ ] 1.9 Hold the physical Shift key: the on-screen Shift keys highlight and the labels change to their shifted symbols; release and they revert.
- [ ] 1.10 Tap **Enter**, **Tab**, **Bksp**, **Del**, the arrows, **Home**, **End**: each acts as expected in Notepad.
- [ ] 1.11 Tap **⊞** (Win) then `r`: the Run dialog opens. Close it.
- [ ] 1.12 In a browser address bar, type a URL with `/` and `.` and press Enter: it navigates.

## 2. Input language

Only if a second keyboard layout is installed (Settings → Time & language → Language).

- [ ] 2.1 Switch Notepad's input language with Win+Space. Within a second the OpenOSK labels change to the new layout (for example `q`→`a` on AZERTY, `y`↔`z` on QWERTZ).
- [ ] 2.2 Typing produces the characters shown on the keys, not the US ones.
- [ ] 2.3 On a layout with dead keys (e.g. US-International), the dead key shows the accent, and pressing it then a vowel produces the accented letter.

## 3. Numeric key pad

- [ ] 3.1 Options → tick *Turn on numeric key pad* → OK. A 4-column pad appears between the letters and the command column.
- [ ] 3.2 Tap `7`, `+`, `2`, `Enter` in Calculator: the result is 9.
- [ ] 3.3 With Num Lock off (tap **NumLk**, indicator goes dark), `7` moves the caret in Notepad like Home.

## 4. Command column

- [ ] 4.1 **Mv Up** moves the keyboard to the top of the current monitor's work area; **Mv Dn** to the bottom.
- [ ] 4.2 **Dock**: the keyboard goes full-width at the bottom edge, Dock stays highlighted, and a maximised Notepad shrinks so its bottom is no longer covered. Title-bar drag and window resize are disabled while docked.
- [ ] 4.3 **Dock** again: the keyboard returns to its previous size and place, and Notepad grows back.
- [ ] 4.4 **Fade**: the keyboard becomes translucent when the pointer leaves it and opaque when the pointer is over it. Fade again returns it to solid.
- [ ] 4.5 **Nav**: a compact layout with arrows, Home/End/PgUp/PgDn, Tab, Esc, Enter, Del, Insert, Space, modifiers and F1–F12 replaces the full keyboard. The title bar says "Navigation". **Gen** returns.
- [ ] 4.6 The Nav layout and the full layout each remember their own window size and position after switching back and forth.
- [ ] 4.7 **Help** opens the help window; it can be closed. **Options** opens the options dialog.
- [ ] 4.8 Options → untick *Show keys to make it easier to move around the screen*: the command column disappears; the gear button in the title bar still opens Options.

## 5. Word prediction

- [ ] 5.1 Type `th` in Notepad via OpenOSK: suggestions such as `the`, `that`, `this` appear above the keys.
- [ ] 5.2 Click `that`: Notepad shows `that ` (with a trailing space, because *Insert space after predicted words* is on).
- [ ] 5.3 Type `Th`: suggestions are capitalised (`That`). Type `TH`: suggestions are `THAT`.
- [ ] 5.4 Type a made-up word such as `zorblax`, then space. Type `zo`: `zorblax` is now the first suggestion (learning). `%LOCALAPPDATA%\OpenOSK\learned-words.txt` contains `zorblax	1`.
- [ ] 5.5 Options → *Forget learned words* → OK. Typing `zo` no longer suggests it.
- [ ] 5.6 Options → untick *Show text predictions* → OK. The suggestion row disappears.

## 6. Hover mode

- [ ] 6.1 Options → *Hover over keys*, duration 1.0 s → OK. The title bar says "Hover".
- [ ] 6.2 Point at `a` and hold still: a progress bar fills along the bottom of the key over one second and `a` is typed once. Keeping the pointer there does not repeat it.
- [ ] 6.3 Moving off a key before the bar fills cancels it.
- [ ] 6.4 Hover **Shift** then hover `b`: `B` is typed.
- [ ] 6.5 Clicking still types too.

## 7. Scan mode

- [ ] 7.1 Options → *Scan through keys*, speed 1.0 s, both *Keyboard key* (Space) and *Mouse click* ticked → OK. The title bar says "Scan".
- [ ] 7.2 Rows are highlighted one after another, top to bottom, wrapping, including the command column.
- [ ] 7.3 Press the physical Space bar while the letter row is highlighted: scanning moves to the keys of that row one by one.
- [ ] 7.4 Press Space on `f`: `f` is typed in Notepad, and scanning returns to rows.
- [ ] 7.5 Let a selected row scan without pressing anything: after two passes it returns to row scanning.
- [ ] 7.6 Clicking anywhere on the keys area selects, the same as Space.
- [ ] 7.7 Physical Space does **not** type a space in Notepad while scan mode is on (it is consumed as the scan key).
- [ ] 7.8 Options → back to *Click on keys*: physical Space types spaces again.

## 8. Appearance

- [ ] 8.1 Options → Theme → Dark, then Light: colours change immediately, including the options dialog.
- [ ] 8.2 Theme → *Follow Windows setting*, then switch Windows to dark mode (Settings → Personalization → Colors): OpenOSK follows within a couple of seconds.
- [ ] 8.3 Turn on a high-contrast theme (Left Alt + Left Shift + Print Screen): OpenOSK uses system colours. Turn it off again.
- [ ] 8.4 Resize the window by its edges: keys and labels scale; nothing is clipped at the minimum size or at full width.
- [ ] 8.5 On a multi-monitor setup with different scaling, drag the keyboard between monitors: it re-scales cleanly and **Mv Up/Dn** use the monitor it is on.
- [ ] 8.6 Options → untick *Use click sound*: keys are silent. Tick again: a short click per key.

## 9. Persistence and startup

- [ ] 9.1 Move and resize the keyboard, wait three seconds, close it, reopen: same place and size.
- [ ] 9.2 `%LOCALAPPDATA%\OpenOSK\settings.json` is readable JSON with the options you set.
- [ ] 9.3 Options → *Start OpenOSK when I sign in* → OK. `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` has an `OpenOSK` value pointing at the exe. Untick → the value is gone.
- [ ] 9.4 `OpenOSK.exe --nav` starts in navigation mode; `OpenOSK.exe --dock` starts docked.
- [ ] 9.5 Options → tick *Start docked* and *Start in navigation mode*: next launch honours both.

## 10. Robustness

- [ ] 10.1 Type continuously for a minute with click sound and prediction on: no lag builds up, and the physical keyboard keeps working normally in another window the whole time.
- [ ] 10.2 Open an elevated app (Run → `notepad` as administrator). Typing into it from OpenOSK does nothing, with no crash. This is expected; see docs/feature-parity.md.
- [ ] 10.3 Corrupt `settings.json` (e.g. replace its contents with `{`) and start OpenOSK: it starts with defaults and rewrites the file on the next change.
- [ ] 10.4 Sign out and back in with start-at-sign-in enabled: exactly one keyboard appears.

## Reporting

Include: Windows version and build, x64 or Arm64, the step number, what you expected, what
happened, the typing mode, and `error.log` if it exists. Never include `learned-words.txt`.
