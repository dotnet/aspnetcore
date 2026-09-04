# PR Attention Queue canvas prototype

A project-scoped Copilot App canvas that renders the existing deterministic
`pr-attention-queue` skill. The extension does not classify, score, or query pull
requests itself. It invokes the skill's PowerShell entry point and renders the
returned JSON.

## Architecture

- **EVIDENCE:** Copilot CLI discovers `extension.mjs` in immediate children of
  `.github/extensions/`, forks each extension as a Node process, and provides
  `@github/copilot-sdk` without a local package install.
- **EVIDENCE:** A canvas registers with
  `joinSession({ canvases: [createCanvas(...)] })`. Its `open` callback returns a
  loopback URL, while declared actions are routed to extension handlers over the
  runtime's JSON-RPC connection.
- **EVIDENCE:** The Aspire Team App binds a per-instance HTTP server to
  `127.0.0.1`, exposes JSON endpoints to its iframe, and uses ordinary
  `node:child_process` `execFile` calls for CLI integrations.
- **INFERENCE:** Running the frozen PowerShell entry point with `execFile` is the
  lowest-divergence integration. The script remains the sole owner of scope,
  classification, ordering, caps, next actors, reason codes, and warnings.

The default input is the offline fixture:

```json
{ "source": "fixture", "preset": "blazor" }
```

The canvas can explicitly refresh from GitHub:

```json
{ "source": "live", "preset": "blazor" }
```

The live path is read-only but slower because it executes the same complete
repository query as the skill.

## Files

| File | Responsibility |
| --- | --- |
| `extension.mjs` | Canvas registration and agent-facing actions. |
| `queue.mjs` | Safe `pwsh` invocation, JSON validation, and agent summary shape. |
| `server.mjs` | Per-instance loopback HTTP server and atomic refresh state. |
| `render.mjs` | Theme-token-based iframe UI. |
| `queue.test.mjs` | Fixture-backed contract test. |

## Deliberate prototype limits

- No GitHub mutation actions.
- No duplicated JavaScript ranking or bucket logic.
- No opaque quality or priority score.
- No automatic live polling; live refresh is explicit.
- No card-to-agent dispatch yet. Aspire demonstrates that pattern by resolving a
  clicked card against server-owned state and then using `session.send` to route
  work to repository-specific skills or a new session. That deserves a separate
  threat-model and UX pass before adoption here.

## Recommended skill changes (not applied)

The prototype works without changing the skill. A production canvas would be
cleaner if the skill later added:

1. A documented JSON compatibility policy for `schemaVersion`, including whether
   new fields are additive and how breaking versions are signaled.
2. A lightweight metadata or preset-discovery command so the UI does not need to
   read `presets.json` directly.
3. A structured progress channel for live queries, separate from JSON stdout, so
   the UI can report query phases during the roughly 40-second refresh.
4. An optional output-file parameter for very large repository-wide snapshots,
   avoiding child-process stdout buffer limits.
5. Stable display labels or descriptions for buckets and reason codes if the UI
   should show friendlier copy without maintaining a second mapping.

These are recommendations only. The seven reviewed skill files remain unchanged.
