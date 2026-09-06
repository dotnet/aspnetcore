# ASP.NET Core Team App

A project-scoped Copilot canvas for the repository's deterministic
`pr-attention-queue` skill. The first mode is intentionally narrow: it helps a
maintainer decide what to review now, what needs rescue, and what is ready to
merge without creating another notification feed.

## Behavior

- Loads live Blazor data by default and supports an explicit whole-repository
  view.
- Keeps `ReviewNow` and `NeedsRescue` as separate primary lanes.
- Adds a dedicated inbox and `Worth reviewing now` summary that exposes the current eligible review digest.
- Surfaces `Recently opened community PRs` in the last seven-day window, including the newest ID and an expandable full recent inventory.
- Shows `Community attention` with `NeedsRescue` items first, followed by the engine-ordered inventory, plus a visible `Unclassified` count and full list.
- Separates ambiguous deterministic Review now candidates into **Verify discussion** rather than
  presenting them as ordinary review work.
- Discloses bounded response evidence and canonical PR links without claiming `no-response` when the evidence is incomplete or ambiguous.
- Shows a compact `ReadyToMerge` strip and expandable secondary
  classifications.
- Preserves the skill's scope, ordering, caps, next actors, reason codes,
  blockers, warnings, and overflow.
- Keeps the last complete snapshot visible while a refresh runs or fails.
- Adds **My PR inbox** for the authenticated user across all open
  `dotnet/aspnetcore` pull requests, with one card per personally signaled PR
  and an expandable full inventory.
- Separates direct review requests, team requests, notification reasons,
  participation, mentions, changed-since-own-review, and evidenced replies in
  participated review threads.
- Displays assessed, partial, unavailable, and unassessed coverage plus cold or
  warm API metrics. Personal signals never grant queue eligibility or create a
  separate follow-up lane; bot-authored and out-of-scope items remain visible
  but are action-withheld.

The canvas does not classify or rank pull requests in JavaScript. It invokes
`Get-PRAttentionQueue.ps1` and validates the skill's versioned JSON contract.
Discussion verification is also supplied by the skill. It surfaces bounded top-level comment
evidence, current thread counts, and explicit truncation signals without changing the canonical
bucket or applying an opaque model judgment. A current unresolved inline thread is routed to
**Verify discussion** because the first-version query does not retrieve inline comment text;
resolved and outdated threads remain factual context rather than an ownership inference.

## Actions

Every visible item can open its canonical pull request in the app's browser.
Only Review now items with a clear bounded discussion assessment can start a new read-only review
session. **Verify discussion** items can be opened but must be interpreted by a human first.
`NeedsRescue`
items can start a new read-only investigation session. The browser sends only
an opaque item ID and action kind; the extension resolves repository, pull
request number, bucket, and URL from the current server-owned snapshot.

The extension has no action that comments, labels, assigns, closes, merges,
rebases, edits files, commits, or pushes.

## Files

| File | Responsibility |
| --- | --- |
| `extension.mjs` | Canvas registration, runtime actions, session dispatch, and browser opening. |
| `queue.mjs` | Safe PowerShell invocation and JSON contract validation. |
| `personal.mjs` | Read-only GitHub-derived personal inbox collection and coverage. |
| `state.mjs` | Atomic snapshots, refresh coalescing, opaque IDs, and action eligibility. |
| `server.mjs` | Loopback HTTP/SSE server and same-origin request boundary. |
| `agent.mjs` | Fixed read-only review and rescue prompts. |
| `render.mjs` | Theme-token-based iframe UI. |
| `*.test.mjs` | Fixture-backed contract, state, security, action, and renderer tests. |

## Deliberate first-version limits

- No GitHub or repository mutation.
- No opaque quality or priority score.
- No inference that a timestamp-only author response makes a PR unconditionally review-ready.
- No claim that a current unresolved inline thread is semantically clear without its comment text.
- No automatic interpretation of truncated discussion history.
- No automatic polling.
- No issue triage, shipping, or repository-health modes yet.
- No testing, CI diagnosis, rebase, conflict resolution, or merge actions.
- No multi-account or durable cross-session snapshot storage.
