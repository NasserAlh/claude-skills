# Board specification

Contents: [Section order](#section-order) · [Status vocabulary](#status-vocabulary) ·
[Open-item tags](#open-item-tags) · [Writing rules](#writing-rules) ·
[Visual identity](#visual-identity) · [Projects without gates](#projects-without-gates)

## Section order

Build the page in this order. It runs from "what is true" through "what you must
do" to "where it is written down" — an approver reading top to bottom gets the
decision before the detail.

1. **Header strip** — phase · branch · commit · tests · updated. One line,
   monospace figures. Add `dirty tree` if the working tree has uncommitted changes.

2. **Right now** — three short paragraphs, in this order: the state the project
   is actually in; what *cannot* be proven from this machine; and anything a
   source document asserts that the repository contradicts. Drop the third only
   when there is genuinely no contradiction to report — never to keep the
   section tidy. This is the only free-prose section; it carries the judgement
   the tables cannot.

3. **Your next action** — a single concrete thing only the approver can do.
   One action, not a list. If there are genuinely several, name the one that
   unblocks the most and say what it unblocks. If the next action belongs to the
   implementer rather than the approver, say that instead of inventing work.

4. **Gates** — the ladder, in order, each with its date and outcome. The open
   gate shows its criteria count (`2 of 5`). Gates not yet started are visibly
   inert, not amber.

5. **What the open gate still needs** — one row per unmet criterion, numbered,
   each with a status word from the vocabulary below and one or two sentences
   saying what was actually observed and why it falls short. A criterion with no
   explanation is not a criterion, it is a wish.

6. **Open and unresolved** — defects, deferred items, decisions waiting on the
   approver, housekeeping. Each tagged (see below).

7. **Recent activity** — newest first, dated, five to eight entries. Each entry
   says what happened and what it changed, not just that something happened.

8. **Where things are written down** — the document map. Each source with a
   one-line statement of what it holds. Always ends with the reminder that the
   board summarises these and never replaces them.

## Status vocabulary

Use these words exactly. The distinctions are the point — collapsing them into
pass/fail is what makes ordinary dashboards useless for sign-off, because it
hides the difference between "we tried and it failed" and "we never tried."

| Word | Means | Colour |
|---|---|---|
| `MET` | Proven, with the evidence recorded in the evidence document | green |
| `CLOSED` | Settled and no longer re-testable — closed by ruling or by design change | green if it settled a pass, inert grey if it settled a failure |
| `PROVISIONAL` | Observed, but on a superseded build or under conditions since changed | amber |
| `AWAITING RE-TEST` | Fixed in code, never yet seen working in the target environment | amber |
| `BLOCKED` | Cannot be attempted until something else lands; name the blocker | red |
| `NOT RUN` | Never attempted | red |
| `NOT MEASURED` | Attempted, but the measurement taken was not the one required | red |

`CLOSED` takes its colour from the outcome it settled, never from the fact of
being settled: green where a pass was made permanent, inert grey where a failure
was ruled shut. An approver reads the pill, not the row beneath it, so a single
green for both says a gate succeeded when it may have been abandoned. It is
never red — red means someone must act, and the point of a closed gate is that
nobody need act on it again.

`PROVISIONAL` is earned by a change to the code the evidence exercised, not by
the fact that HEAD has moved. A commit touching only configuration or
documentation supersedes nothing, and downgrading on every commit drains the
word as thoroughly as never downgrading does — a MET that decays whenever
anyone commits stops distinguishing evidence that still holds from evidence
that does not, and the approver learns to ignore it. So check what the commit
actually touched. When a gate stays MET across a moved HEAD, the row names the
commit that moved it and says why that commit was inert, so the reader can see
the judgement was made rather than wonder whether it was skipped.

`NOT MEASURED` earns its own word because it is the easiest self-deception on a
status board: a number was produced, it looked like evidence, and it measured
the wrong thing. Recording it as MET is how a gate gets signed off on a number
nobody checked the definition of.

## Open-item tags

| Tag | Means | Colour |
|---|---|---|
| `AWAITING RE-TEST` | Defect fixed in code, unseen in the target environment | amber |
| `YOUR CALL` | Blocked on an approver decision, not on work | amber |
| `HOUSEKEEPING` | Real but not gating — worth a check at merge or release | inert |
| `DEFERRED` | Deliberately postponed; say what it depends on | inert |

`YOUR CALL` is amber, not red: a decision waiting on the approver is not a
failure, and colouring it red puts the board's alarm on the one item whose only
requirement is that someone reads it. Amber says "this is where the work stops
until you act", which is the truth of it.

## Writing rules

**Write for someone who has been away for two weeks.** No unexpanded internal
shorthand on first use — a change-note id or defect id gets a clause saying what
it did. `CN-D6` means nothing to the approver in six months; "CN-D6 — only
support and resistance retire; references are exempt" survives.

**Quote the observation, not the conclusion.** "Analysis returned in 38 s and
drew 7 correct labels, but the lines collapsed to one bar wide" beats "rendering
defect found." The approver can re-derive the conclusion; they cannot re-derive
the observation.

**Say what was superseded.** When evidence ran against a build that no longer
exists, that fact belongs in the row, not in a footnote. It is the difference
between PROVISIONAL and MET.

**Never soften a red.** No "nearly there", no "minor remaining item." If three
of five criteria need a live environment, the board says three of five need a
live environment.

**Date everything.** Every gate outcome, every activity entry, every measurement.
An undated claim on a status board decays into a rumour.

## Visual identity

Take `title`, `favicon` and `palette` from `.claude/gate-board.json` and keep
them identical across refreshes. Where a project has its own colour meanings —
a charting study's level colours, a brand palette — draw the board's palette
from those, so the page reads as part of the project rather than a generic
dashboard.

Map the palette consistently: passed reads green, provisional and awaiting-retest
read amber, blocked and not-run read red, not-started reads inert grey. Do not
introduce a colour that carries no meaning.

Keep it one page. Scrolling is fine; a second page is not, because the value of
the board is that it can be taken in whole.

## Projects without gates

When the config has `"gates": false`, drop sections 4 and 5 and replace them with
a single **Open work** section: what is in progress, what is blocked and on
what, what is done since the last refresh. Everything else — the header, right
now, your next action, open items, recent activity, document map — stays.

The spine of the board is *current state, what is blocked on you, what changed*.
Gates are one way of expressing that, not the point of it. A small project with
no ladder still benefits from the honest status vocabulary above; apply the same
words to tasks that the gated version applies to criteria.
