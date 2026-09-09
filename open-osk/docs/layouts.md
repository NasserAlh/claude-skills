# Layout files

A layout is a JSON document with three blocks: the main `rows`, an optional `numPad`, and the
`commands` column shown to the right. Each block is an array of rows; each row is an array of
keys. Widths are in key units (a letter key is `1`); every row in a block should add up to the
same total so the keys line up.

```json
{
  "id": "my-layout",
  "name": "My layout",
  "rows": [
    [ { "vk": "Escape", "label": "Esc" }, { "vk": "D1", "char": true, "fn": "F1" }, { "gap": 0.5 }, { "vk": "Back", "w": 2 } ],
    [ { "mod": "Shift", "w": 2 }, { "vk": "Z", "char": true }, { "cmd": "Options" } ]
  ],
  "numPad": [ [ { "vk": "NumPad7" }, { "vk": "NumPad8" } ] ],
  "commands": [ [ { "cmd": "Navigation" } ], [ { "cmd": "Fade" } ] ]
}
```

## Key fields

Exactly one of `vk`, `mod`, `cmd` or `gap` decides what the key is.

| Field | Meaning |
|---|---|
| `vk` | A virtual-key name from `VirtualKey` (`A`, `D1`, `Return`, `Oem3`, `NumPad7`, `F5`...). |
| `char` | `true` if the key produces a character. Its label then follows the active input language, and `label` is only a fallback. |
| `fn` | Virtual key to send instead while Fn is active (used for the number row → F1..F12). |
| `mod` | `Shift`, `Control`, `Alt`, `Win` or `Fn`. Makes a sticky modifier. Add `vk` to pick a side (`RShift`, `RControl`, `RMenu`). |
| `cmd` | `Navigation`, `General`, `MoveUp`, `MoveDown`, `Dock`, `Fade`, `Options`, `Help`, `NumPad`. |
| `gap` | Width of an empty space. |
| `label` | Text on the key. Defaults are provided for common keys. |
| `shift` | Small secondary label in the top-right corner. Character keys compute it automatically. |
| `w` | Width in key units. Default `1`. |
| `ext` | Force the extended-key flag on or off. Derived automatically for arrows, Ins/Del/Home/End/PgUp/PgDn, right Ctrl/Alt, Win, numpad `/`. Set `"ext": true` on a numpad Enter. |
| `lock` | Force lock-key behaviour (indicator mirrors the real lock state). Automatic for Caps Lock, Num Lock, Scroll Lock. |
| `repeat` | Whether holding the key auto-repeats. Default `true` for character and action keys. |
| `id` | Optional stable id; needed only when two keys would otherwise get the same generated id (for example two Shift keys). |

The `commands` block has one row per main row; the command in row *n* is drawn beside main
row *n*. Keys of any kind may also appear inside the main rows, which is how the standard layout
puts Options and Help on the bottom row.

## Installing a layout

Save the file as `%LOCALAPPDATA%\OpenOSK\layouts\<id>.json`, open Options and choose it under
*Keyboard → Layout*. A file that fails to parse is skipped with no error so the keyboard always
starts; validate your file with the tests in `tests/OpenOsk.Core.Tests/LayoutTests.cs` or by
loading it with `LayoutParser.Parse` in a small script.
