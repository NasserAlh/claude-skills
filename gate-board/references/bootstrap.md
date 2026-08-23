# Bootstrap — standing a board up in a new repository

Read this when `.claude/gate-board.json` does not exist. The goal is a config the
approver has actually looked at, not a config you inferred and published from.

## 1. Discover, do not assume

Look for the controlled documents. Typical locations, in order of likelihood:

- Control document: `CLAUDE.md`, `AGENTS.md`, `docs/CONTROL.md`
- Requirements: `docs/REQ-*.md`, `docs/requirements*.md`, `docs/PRD*.md`
- Design: `docs/DES-*.md`, `docs/design*.md`, `docs/ARCHITECTURE.md`
- Evidence: `docs/VERIFICATION.md`, `docs/EVIDENCE.md`, `docs/TEST-*.md`

Also establish, by reading rather than guessing:

- Does this project run a **gate ladder**? Look for gate ids, sign-off language,
  or a dated gate log. If there is none, set `"gates": false` — do not impose a
  ladder on a project that does not have one.
- What is the **test command**? Read the build file. Maven → `mvn -B test`,
  Gradle → `./gradlew test`, uv → `uv run pytest -q`, npm → `npm test`.

  **Never pick a flag that suppresses the pass/fail count.** Maven's `-q`
  silences INFO, which is the level Surefire prints `Tests run: …` at — the
  command succeeds, prints nothing readable, and the board silently falls back
  to a stale figure from the evidence file. Use `-B` instead: non-interactive
  and no colour codes, but the counts survive. Whatever the toolchain, run the
  command once during bootstrap and confirm you can actually read numbers off
  the output before writing it into the config.

  Prefer the invocation the project's own build script uses, so the board and
  the developer see the same suite. If that script runs tests requiring a
  network, a key or paid API calls, exclude those — a board refresh should
  never cost money or touch a live service.

  If the suite is slow enough that running it on every refresh would be
  annoying, set `test_command` to `null` and the board will attribute the last
  recorded figure to its commit instead.

## 2. Show the user what you found, before writing anything

Report the discovered document set, the gate answer, and the proposed test
command. Ask for corrections. Missing the evidence document, or pointing at a
superseded requirements version, produces a board that is confidently wrong —
and the approver has no way to tell from the page itself.

## 3. Write the config

Create `.claude/gate-board.json`:

```json
{
  "title": "<Project> Gate Board",
  "favicon": "<one or two emoji>",
  "board_url": "",
  "gates": true,
  "test_command": "<the command you ran and read counts off, or null>",
  "footer": "<one line: what the project is, and its language and build tool>",
  "palette": {
    "passed":      "#2e7d4f",
    "provisional": "#b8860b",
    "blocked":     "#c0392b",
    "inert":       "#8a8a8a",
    "accent":      "#2b6cb0"
  },
  "sources": {
    "control":      "CLAUDE.md",
    "requirements": "<path>",
    "design":       "<path>",
    "evidence":     "<path>"
  }
}
```

Every value in angle brackets is a placeholder — replace it with what you
discovered in step 1. The palette is a sane default and can stay as-is, or be
drawn from the project's own colour meanings.

**The `sources` keys are descriptive labels you choose per project, not a fixed
four-slot schema.** Name each key after what that document actually is. A
project with no requirements baseline and no design document should not invent
them to fill the slots: keys like `guide`, `procedure` or `archive` describe a
user guide, an operating routine and a frozen record far more honestly, and a
board that names a stretchy source is the first step toward a board that is
confidently wrong. Only `control` and `evidence` are near-universal.

Leave `board_url` empty. It gets filled in after the first publish, not before.

**Commit this file.** It is the repo's binding to its own board, and it needs to
survive a fresh clone on another machine. The repository is the sync mechanism —
a registry kept in `~/.claude` would only work on the machine that wrote it.

The URL is not a secret in the usual sense — the artifact stays private to the
account that owns it until shared — but it does identify a private page. On a
repository with a public remote, keep `.claude/gate-board.json` out of the
public mirror, or leave `board_url` empty there and hold it in a local,
git-ignored `.claude/gate-board.local.json` that the skill reads in preference.

## 4. Build, show, approve, publish

Build the board per `board-spec.md` and show the user the content **before**
publishing anything. First boards are where wrong document mappings surface —
a gate ladder read out of a superseded requirements file looks entirely
plausible until the approver reads it.

On approval, publish. Then write the returned URL into `board_url` and commit
the config with a message naming the board.

## 5. Leave a procedure note in the repo

Write `docs/STATUS-BOARD.md` (or alongside the other docs) saying: what the board
is, its URL, that saying "refresh the gate board" rebuilds it, that it is a
rendering and never a source of truth, and when to refresh — after a gate
attempt, after a ruling or change note, after a merge to main, before handing
the project on, or after a long gap.

The note exists for the human, not for this skill. Someone opening the repo in a
year needs to know the board exists and that it is downstream of the documents;
that fact lives nowhere else in the repository.
