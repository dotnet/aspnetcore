# ASP.NET Core Team App

A project-scoped Copilot canvas for the repository's deterministic
`pr-attention-queue` skill. The first mode is intentionally narrow: it helps a
maintainer decide what to review now, what needs rescue, and what is ready to
merge without creating another notification feed.

## Behavior

- Loads live Blazor data by default and supports an explicit whole-repository
  view.
- Keeps `ReviewNow` and `NeedsRescue` as separate primary lanes.
- Shows a compact `ReadyToMerge` strip and expandable secondary
  classifications.
- Preserves the skill's scope, ordering, caps, next actors, reason codes,
  blockers, warnings, and overflow.
- Keeps the last complete snapshot visible while a refresh runs or fails.

The canvas does not classify or rank pull requests in JavaScript. It invokes
`Get-PRAttentionQueue.ps1` and validates the skill's versioned JSON contract.

## Actions

Every visible item can open its canonical pull request in the app's browser.
`ReviewNow` items can start a new read-only review session, and `NeedsRescue`
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
| `state.mjs` | Atomic snapshots, refresh coalescing, opaque IDs, and action eligibility. |
| `server.mjs` | Loopback HTTP/SSE server and same-origin request boundary. |
| `agent.mjs` | Fixed read-only review and rescue prompts. |
| `render.mjs` | Theme-token-based iframe UI. |
| `*.test.mjs` | Fixture-backed contract, state, security, action, and renderer tests. |

## Deliberate first-version limits

- No GitHub or repository mutation.
- No opaque quality or priority score.
- No automatic polling.
- No issue triage, shipping, or repository-health modes yet.
- No testing, CI diagnosis, rebase, conflict resolution, or merge actions.
- No multi-account or durable cross-session snapshot storage.
