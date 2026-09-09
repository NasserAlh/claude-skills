# Changelog

All notable changes to OpenOSK are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed

- Holding a key now repeats at the rate set in Windows keyboard settings. The repeat timer ticked
  late, so a key repeated at about 22 per second instead of 30.
- Numeric key pad keys act as Home, End, the arrows, Insert and Delete while Num Lock is off, as
  on a physical keyboard. They always typed digits.
- Fade now makes the keyboard translucent. WPF strips the layered window style the previous
  implementation set, so the window uses WPF's own opacity instead.
- Learned words are written to disk a couple of seconds after they are learned. They were only
  saved when the window moved, resized or closed, so a crash lost them.
- In hover mode, closing the Options dialog no longer dwell-types (or reopens Options from) the
  key the pointer happens to rest on.
- Long key labels (Options, PrtScn, Mv Up, Insert...) shrink to fit their key instead of being cut
  to "Op...", and the minimum window height is 200 so the bottom row is never clipped.
- The word-suggestion row sizes itself to its chips; at a fixed height the words were drawn half
  under the key rows.
- The small corner label (the shifted symbol) is hidden when the keys are too small for two
  labels, instead of overlapping the main label.

## [0.1.0] - 2026-09-07

First release.

### Added

- Standard keyboard with Fn row, sticky modifiers, lock indicators, auto-repeat and an optional
  numeric key pad.
- Labels that follow the active input language of the foreground application.
- Click, hover (dwell) and scan typing modes with the same ranges as the Windows OSK.
- Nav / Gen, Mv Up, Mv Dn, Dock (shell app bar) and Fade commands.
- Local word prediction with learned words, "insert space after prediction" and a forget button.
- Options dialog mirroring the Windows OSK plus theme, fade opacity, key repeat, modifier lock,
  custom layouts and start-at-sign-in.
- Light, dark and high-contrast themes following the Windows setting.
- Single-instance behaviour, `--nav` and `--dock` switches.
- Self-contained single-file builds for x64 and Arm64 from GitHub Actions.
