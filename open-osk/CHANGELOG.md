# Changelog

All notable changes to OpenOSK are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

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
