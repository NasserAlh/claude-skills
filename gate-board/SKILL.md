---
name: gate-board
description: Rebuild and republish this repository's approver status board — a one-page snapshot of current phase, gate ladder, what the open gate still needs, unresolved items, and recent activity. Use whenever the user says "refresh the gate board", "refresh the status board", "update the board", "rebuild the board", or asks where the project stands, what is blocked, what is still unproven, or what only they can do next. Also use proactively after a gate attempt (passed or failed), after a ruling or change note, after a merge to main, before handing the project to anyone else, or when returning to a repository after a long gap. Use it even if the user does not say the word "board".
---

# Gate Board

A rendering of state that lives elsewhere. **It decides nothing.**

The board is written for the **approver, not the implementer**: what is true right
now, what is still unproven, and what only the approver can decide or observe.
Every line traces to a controlled document in this repository. Nothing is
inferred, softened, rounded, or recalled from session memory.

This skill is repo-agnostic. Everything project-specific — which documents are
authoritative, where the board is published, what the gate ladder is called —
lives in `.claude/gate-board.json` in the repository — optionally overridden by
a git-ignored `.claude/gate-board.local.json` alongside it — not here.

## Step 1 — Read the repo binding first

Read the repo binding from the repository root before anything else, in this
order:

1. `.claude/gate-board.local.json` — read it first if it exists.
2. `.claude/gate-board.json` — the tracked config, and the fallback.

When both are present, merge them and let the local file win on any key it
sets, `board_url` above all. The local file exists because the tracked config
may be committed to a repository with a public remote, where the board address
should not travel: the public copy leaves `board_url` empty and the real address
lives in the git-ignored local file. Treat a `board_url` found in either file as
"present, set" — missing that is exactly how a second, competing board gets
published.

| Config state | What to do |
|---|---|
| **Neither file exists** | This repo has no board. Read `references/bootstrap.md`. Stop at the approval step — **do not publish.** |
| **`board_url` empty or absent in both** | Configured but never published. Build it, show the user, get explicit approval, publish, then write the returned URL back into the config — into the local file if this repo uses one. |
| **`board_url` set in either file** | Refresh in place. Pass that exact address as `url` when publishing. |

**Why this matters:** publishing without the configured URL creates a second,
competing board. Two boards disagreeing about a gate outcome is worse than no
board at all, because both look authoritative. If the config is unreadable, or
the repo you are in is not the one the config describes, stop and say so. Never
guess a URL, never reuse a URL seen in another repository or earlier in the
session, and never publish "just to see how it looks."

## Step 2 — Collect the header facts by observation

The header strip carries five facts. Each one is either observed in this session
or attributed to the commit where it was last observed. Never carry one forward
silently.

- **Phase** — from the control document's current-scope section.
- **Branch** — `git rev-parse --abbrev-ref HEAD`
- **Commit** — `git rev-parse --short HEAD`
- **Tests** — run the config's `test_command` and read the real numbers off the
  output. If `test_command` is `null`, take the figure from the evidence
  document and label it with the commit it was recorded at, so a stale number
  is visibly stale rather than quietly wrong.
- **Updated** — today's date.

If the working tree is dirty, say so in the header. A board generated from
uncommitted work describes a state no one else can check out.

## Step 3 — Check whether the sources are canonical yet

Many projects declare a registry, baseline or change log authoritative on one
particular branch — usually `main`. If the control document makes a declaration
like that, read it literally: a board built from a feature branch is rendering
rulings that are **not yet canonical by the project's own rule**.

Check it before building:

- Compare `git rev-parse --abbrev-ref HEAD` against the declared branch.
- If they differ, diff the registry itself:
  `git show <authoritative-branch>:<control-document>` against the working copy,
  and list which change notes, rulings or gate outcomes exist only on this branch.

Report the difference on the board as **housekeeping**, naming the specific
rulings and saying it is a merge-time check. It is not a blocker — work in
progress on a branch is normal — but it is exactly the thing that disappears
quietly in a bad merge, and an approver signing a gate off a branch board is
signing off on rulings the registry does not yet recognise.

If the control document declares no authoritative branch, skip this step.

## Step 4 — Build from the sources, never from memory

The config's `sources` map names the controlled documents. Read **all** of them
in this session before writing a single line of the board. The typical set:

- **control** — rules, the change-note registry, current scope, the dated gate log
- **requirements** — phases and the gate each phase must pass
- **design** — components, contracts, design rulings and their change log
- **evidence** — every claim proven, with the output that proved it; defects live here

Some repos have fewer. Read whatever the config names, and if a named file is
missing, say which one and build the board without that section rather than
filling the gap from inference.

Then write the board to the structure and status vocabulary in
`references/board-spec.md`. Read that file now — it carries the section order,
the status words and what separates them, and the writing rules that keep the
board honest.

## Step 5 — Publish and report

Publish to `board_url` from the config. Then tell the user, briefly:

- what changed on the board since the last refresh
- anything a source document asserts that the repository contradicts
- any gate criterion whose status you had to downgrade

That last one matters most. A criterion that was MET last time and is
PROVISIONAL now — because the build it was proven on has been superseded — is
the single most useful thing a refresh can surface, and the easiest to miss.

## The rules that keep this trustworthy

1. **The board is never a source of truth.** If it disagrees with the controlled
   documents, the documents win and the board is stale. A decision, defect, or
   gate outcome recorded only on the board does not exist.

2. **Never write a decision to the board.** If the user says something during
   the refresh that amounts to a ruling, a gate sign-off, or a new defect, that
   belongs in the controlled documents first. Offer to record it there, then
   refresh. Do not let the board become the place where things get decided.

3. **Do not upgrade a status to be encouraging.** NOT RUN is not "in progress."
   PROVISIONAL is not MET. An approver reading an inflated board makes a bad
   sign-off decision, and the board's only job is to prevent that.

4. **Absence is reportable.** If a document does not say something, the board
   says "not recorded" — not a guess, not a plausible reconstruction.

5. **Keep the visual identity stable** across refreshes: same title, favicon and
   palette from the config, so a returning reader recognises the page and reads
   the colours the same way every time.
