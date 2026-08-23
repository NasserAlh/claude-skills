# claude-skills

Claude Code skills I use on my own projects, published in case they are useful to
anyone else. Currently one skill: **gate-board**.

---

## gate-board

A gate board is a one-page status snapshot written for **the person who signs
work off**, not for the person doing the work. It answers four questions:

- What is true in this repository right now?
- What is still unproven, and why?
- What is blocked, and on what?
- What is the one thing only the approver can do next?

The skill rebuilds that page from the repository's own controlled documents and
republishes it to the same address every time, so a returning reader always finds
the current state at a URL they already have.

**It decides nothing.** The board is a rendering of state that lives elsewhere.
If the board and the documents disagree, the documents win and the board is
stale. A decision, defect or gate outcome recorded only on the board does not
exist.

### Who it is for

Anyone who has to approve something they did not personally build, and who keeps
running into the same failure: a dashboard that is green because nobody ran the
test, or because the number that was measured was not the number the criterion
asked for. It fits regulated, audited or handover-heavy work best — but the
`"gates": false` mode drops the gate ladder and keeps the rest, which works fine
for an ordinary project that just wants an honest weekly state page.

### What the board contains

Header strip (phase · branch · commit · tests · updated) → *Right now* →
*Your next action* → the gate ladder → what the open gate still needs →
open and unresolved items → recent activity → the document map.

It runs from "what is true" through "what you must do" to "where it is written
down", so an approver reading top to bottom gets the decision before the detail.

---

## The three-way split

The design rests on keeping three different kinds of thing in three different
places. Everything else follows from this.

| Layer | Lives in | Changes when |
|---|---|---|
| **The procedure** — how to build a board, in any repository | `gate-board/SKILL.md` | The method improves. Portable; nothing project-specific belongs here. |
| **The binding** — which documents are authoritative *here*, where this repo's board is published, what its test command is | `.claude/gate-board.json`, committed to your repo | This project's documents move, or its board is first published. |
| **The editorial stance** — the status words, the section order, the writing rules, what colour means what | `gate-board/references/board-spec.md` | Rarely. This is the part that makes the output honest. |

Why the split matters:

- The **skill is repo-agnostic**, so one copy in `~/.claude/skills/` serves every
  project on the machine. It contains no paths, no URLs, no project names.
- The **binding is committed to the repository**, not stored in `~/.claude`. The
  repo is the sync mechanism: a fresh clone on another machine finds its own
  board. A registry in a home directory would only work on the machine that
  wrote it.
- The **stance is separated from the procedure** because it is the part worth
  arguing with. If you disagree that `NOT MEASURED` deserves its own status word,
  you edit one reference file and the procedure keeps working.

There is one optional fourth piece: a git-ignored `.claude/gate-board.local.json`
that overrides the committed config. It exists so a repo with a public remote can
commit `board_url: ""` and keep the real address out of the public copy.

---

## Status vocabulary

This is the useful part even if you never install the skill. Most status
dashboards collapse everything into pass / fail, which hides the difference
between *we tried and it failed* and *we never tried*. These seven words keep
that difference visible:

| Word | Means | Colour |
|---|---|---|
| `MET` | Proven, with the evidence recorded in the evidence document | green |
| `CLOSED` | Settled and no longer re-testable — closed by ruling or by design change | green if it settled a pass, inert grey if it settled a failure |
| `PROVISIONAL` | Observed, but on a superseded build or under conditions since changed | amber |
| `AWAITING RE-TEST` | Fixed in code, never yet seen working in the target environment | amber |
| `BLOCKED` | Cannot be attempted until something else lands; name the blocker | red |
| `NOT RUN` | Never attempted | red |
| `NOT MEASURED` | Attempted, but the measurement taken was not the one required | red |

Three of these carry most of the weight:

- **`NOT MEASURED`** earns its own word because it is the easiest self-deception
  on a status board: a number was produced, it looked like evidence, and it
  measured the wrong thing. Recording that as `MET` is how a gate gets signed off
  on a figure nobody checked the definition of.
- **`PROVISIONAL`** is earned by a change to the code the evidence exercised, not
  by the fact that HEAD has moved. A commit touching only config or docs
  supersedes nothing. Downgrading on every commit drains the word as thoroughly
  as never downgrading does — a `MET` that decays whenever anyone commits stops
  distinguishing evidence that still holds from evidence that does not, and the
  approver learns to ignore it.
- **`CLOSED`** takes its colour from the outcome it settled, never from the fact
  of being settled. One green for both says a gate succeeded when it may have
  been abandoned. It is never red: red means someone must act, and the point of a
  closed gate is that nobody need act on it again.

Open items get their own four tags — `AWAITING RE-TEST`, `YOUR CALL`,
`HOUSEKEEPING`, `DEFERRED`. `YOUR CALL` is amber rather than red, because a
decision waiting on the approver is not a failure; colouring it red puts the
board's alarm on the one item whose only requirement is that someone reads it.

The accompanying writing rules are in
[`gate-board/references/board-spec.md`](gate-board/references/board-spec.md).
The short version: quote the observation, not the conclusion; say what was
superseded; date everything; never soften a red; never upgrade a status to be
encouraging.

---

## Install

Copy the skill folder into your personal skills directory:

```bash
# macOS / Linux
cp -r gate-board ~/.claude/skills/
```

```powershell
# Windows
Copy-Item -Recurse gate-board "$env:USERPROFILE\.claude\skills\"
```

You should end up with:

```
~/.claude/skills/gate-board/
├── SKILL.md
└── references/
    ├── board-spec.md
    └── bootstrap.md
```

Then open any repository and say:

> refresh the gate board

On a repo with no `.claude/gate-board.json`, the skill runs its bootstrap: it
looks for your control, requirements, design and evidence documents, works out
whether the project has a gate ladder at all, proposes a test command, and
**shows you all of it before writing anything**. It stops before publishing and
waits for you.

On a repo already configured, it rebuilds the board and republishes it to the
same URL.

Other phrasings that trigger it: *update the board*, *rebuild the board*, *where
does this project stand*, *what is still unproven*, *what is blocked*.

### The config it writes

```json
{
  "title": "Example Gate Board",
  "favicon": "🚦",
  "board_url": "",
  "gates": true,
  "test_command": "mvn -B test",
  "footer": "One line: what the project is, its language and build tool",
  "palette": {
    "passed":      "#2e7d4f",
    "provisional": "#b8860b",
    "blocked":     "#c0392b",
    "inert":       "#8a8a8a",
    "accent":      "#2b6cb0"
  },
  "sources": {
    "control":      "CLAUDE.md",
    "requirements": "docs/REQUIREMENTS.md",
    "design":       "docs/DESIGN.md",
    "evidence":     "docs/VERIFICATION.md"
  }
}
```

The `sources` keys are descriptive labels you choose per project, not a fixed
four-slot schema. A project with no design document should not invent one to fill
a slot — name the key after what the document actually is. Only `control` and
`evidence` are near-universal.

Set `test_command` to `null` if your suite is too slow to run on every refresh;
the board will then attribute the last recorded figure to its commit, so a stale
number is visibly stale rather than quietly wrong.

---

## What the board can and can't show you

The board renders what the project's documents already record. There is no
required document set and no minimum.

- A project with a dated gate log gets a gate ladder, each gate carrying its
  date and its outcome.
- A project with an evidence document gets criteria that trace back to real
  observations — the output that was actually seen, not a summary of it.
- A project with neither still gets the spine of the board: what is true right
  now, what is blocked and on what, and what changed since the last refresh.

So a thin board is a finding about the documentation, not a setup step the
reader skipped. Where nothing recorded proves a criterion, the board says
`NOT RUN` against it. Where a named source is missing, the skill says which one
and builds the board without that section. It reports the gap instead of closing
it by inference — and *nobody wrote this down* is a more useful thing for an
approver to read than a full-looking page assembled from guesses.

Which is why **you should not write documents for the board.** A requirements
file created to give the board something to point at is a file nobody maintains;
within a month it is wrong, and the board, faithfully rendering it, is wrong
with it. Sources should exist because the project needs them. The board only
reads them.

### A project with a different document set

The four-document example above is the common shape, not the required one. This
is a real config from a Python data project with no `docs/` directory, no design
document, no requirements baseline and no automated test suite:

```json
{
  "title": "Market Gauge Board",
  "favicon": "🌡️",
  "board_url": "https://claude.ai/code/artifact/...",
  "gates": true,
  "test_command": null,
  "footer": "Weekly market-temperature gauge. Daily bars from a broker API, Python 3.12.",
  "palette": {
    "passed": "#2e7d4f", "provisional": "#b8860b",
    "blocked": "#c0392b", "inert": "#8a8a8a", "accent": "#2b6cb0"
  },
  "sources": {
    "control":   "CLAUDE.md",
    "guide":     "README.md",
    "procedure":  "RUN_SCAN.md",
    "evidence":  "VALIDATION.md",
    "archive":   "validation_archive/README.md"
  }
}
```

Three things it demonstrates:

- **The `sources` keys are named after what each document actually is.**
  `guide`, not `requirements`, because that file is a user guide. Calling it a
  requirements baseline would be the first step toward a board that is
  confidently wrong — the approver would read its contents as commitments the
  project had made to itself.
- **`test_command` is `null` because there is no suite to run.** The board
  reports the manually-run figure and attributes it to the commit it was taken
  at, rather than implying a green run that never happened.
- **`gates` is still `true` without a G0/G1 ladder.** A gate is anything that
  has to be passed before work proceeds. A numbered phase ladder is one form; a
  rule applied to each new candidate before it is accepted is another, and
  `"gates": true` is right for both.

Setting `"gates": false` drops the ladder sections and replaces them with a
single *Open work* section: what is in progress, what is blocked and on what,
what is done since the last refresh.

---

## Security

Installing a skill from a stranger means letting their instructions steer an
agent that has tool access on your machine. That is worth being wary of, so here
is the complete list of what this one executes — and it is checkable, because the
skill is three Markdown files with nothing hidden in them.

| Command | Why |
|---|---|
| `git rev-parse --abbrev-ref HEAD` and `git rev-parse --short HEAD` | Branch and commit for the header strip |
| `git status` | Detect an uncommitted working tree, so the board can say `dirty tree` |
| `git show <branch>:<path>` | Compare a control document against its authoritative branch |
| whatever `test_command` **you** put in your own `.claude/gate-board.json` | Read real pass/fail counts instead of copying a stale number |

That is the entire list. There is nothing else.

- **No scripts.** The skill ships no executables — no `.sh`, `.ps1`, `.py`, no
  `scripts/` directory. It is `SKILL.md` plus two Markdown reference files.
- **No network calls, no installs, no downloads.** It fetches nothing and adds no
  dependencies.
- **No writes outside your repository.** It creates or updates
  `.claude/gate-board.json` and optionally `docs/STATUS-BOARD.md`. It touches
  nothing in your home directory and nothing outside the project.
- **`test_command` is yours.** The skill never invents one and never runs one you
  have not seen: bootstrap proposes a command and shows it to you before writing
  it into the config, and thereafter it runs only what that file says. The
  bootstrap guidance explicitly tells it to exclude tests that need a network, a
  key, or paid API calls — a board refresh should never cost money or touch a
  live service.
- **Publishing is not the skill's doing.** The board page is published through
  Claude Code's own artifact publishing, to a page private to your account until
  you choose to share it. The skill does not shell out anywhere to publish, and it
  will not publish at all without your explicit approval on a first build.

`git status` and `git show` read; they do not write. Nothing in that list mutates
your repository, your history, or your working tree.

---

## Licence

MIT — see [LICENSE](LICENSE).
