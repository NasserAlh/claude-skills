# Feature parity with the Windows On-Screen Keyboard

Reference: `osk.exe` as shipped in Windows 10 and Windows 11. ✅ done, 🟡 partial or different
by design, ❌ not implemented.

## Keyboard

| Windows OSK | OpenOSK | Notes |
|---|---|---|
| Standard 101-key layout with Fn row toggle | ✅ | Fn turns the number row into F1–F12, as in the Windows OSK. |
| Labels follow the active input language | ✅ | `ToUnicodeEx` against the foreground application's layout; updates within 100 ms of a language switch. |
| Shift / Ctrl / Alt / Win sticky (one shot) | ✅ | Plus an optional lock on the second tap (Options → Keyboard). |
| Caps / Num / Scroll Lock indicators | ✅ | Mirror the real toggle state, including changes from a physical keyboard. |
| Physical modifier keys reflected | ✅ | A held physical Shift highlights the on-screen Shift keys and changes the labels. |
| Key auto-repeat while held | ✅ | Uses the delay and rate from Windows keyboard settings. |
| Numeric key pad ("Turn on numeric key pad") | ✅ | |
| Nav layout (Nav / Gen) | 🟡 | Same idea, compact navigation set; the exact key positions differ. |
| Mv Up / Mv Dn | ✅ | Moves to the top/bottom of the work area of the current monitor. |
| Dock | ✅ | Registers a shell app bar so maximised windows shrink to make room. |
| Fade | ✅ | Translucent until pointed at; opacity is configurable. |
| Resizable, keys scale with the window | ✅ | Font size follows key size. |
| Never steals focus | ✅ | `WS_EX_NOACTIVATE` + `MA_NOACTIVATE`. |
| Always on top | ✅ | |
| Per-monitor DPI aware | ✅ | PerMonitorV2 manifest. |

## Options dialog

| Windows OSK | OpenOSK |
|---|---|
| Use click sound | ✅ (synthesised in memory, no audio file) |
| Show keys to make it easier to move around the screen | ✅ (shows/hides the Nav / Mv Up / Mv Dn / Dock / Fade column) |
| Turn on numeric key pad | ✅ |
| Click on keys | ✅ |
| Hover over keys, hover duration 0.5–3 s | ✅ with a progress bar on the key |
| Scan through keys, scanning speed 0.5–3 s | ✅ row then key; two passes then back to rows |
| Scan select: keyboard key | ✅ any of Space, Enter, Tab, Esc, Pause, Scroll Lock, F1–F12 (global hotkey) |
| Scan select: mouse click | ✅ |
| Scan select: joystick / game pad | ❌ not yet |
| Show text predictions | ✅ local word list plus learned words |
| Insert space after predicted words | ✅ |
| Use On-Screen Keyboard before sign-in | ❌ the sign-in screen can only run Microsoft's keyboard |
| Control whether OSK starts when I sign in | ✅ "Start OpenOSK when I sign in" (per-user Run key, no admin) |

## Additions not in the Windows OSK

- Light, dark and high-contrast themes, following the Windows setting by default.
- Custom layouts from JSON files.
- Key labels of the shifted symbol shown in the corner of each key.
- Learned-word list you can inspect and clear; nothing is uploaded.
- Command-line switches `--nav` and `--dock`.
- A second launch brings the running keyboard to the front instead of opening another.

## Known limitations

- **Elevated applications.** Windows blocks `SendInput` into windows running as administrator
  unless the sender is also elevated or has UI Access (which needs a code-signed binary in
  Program Files). Run OpenOSK as administrator when you need to type into such a window.
- **Sign-in screen and UAC prompts.** Only the built-in keyboard can run there.
- **Dead keys** are shown on the key as the character the layout reports for the dead key alone
  (`´` on German, `'` on US-International) and are sent as the real key, so composing accented
  characters works exactly as on a physical keyboard for that layout.
- **Predictions are not scanned** in scan mode; use the mouse for them.
- **32-bit Windows** is not supported. Builds are provided for x64 and Arm64.
