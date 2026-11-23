# L3 local corpus tools

These dependency-free Node scripts follow the existing `.mjs` evaluation tooling. Run from the repository root with Node 22+, Git, and an authenticated `gh` for `outcomes`.

```powershell
node eng\skill-evals\l3\l3.mjs pin --repo dotnet/aspnetcore --run <run-id>-<attempt> --pr <number> --head <sha> --baseTip <sha> --mergeBase <sha> --workflow <sha> --guidance <sha>
node eng\skill-evals\l3\l3.mjs outcomes --repo dotnet/aspnetcore --run <run-id> --attempt <attempt> --output <index-path>
node --test eng\skill-evals\l3\test_l3.mjs
```

- `pin` uses one full-history bare object store and immutable per-run refs. The mirror defaults to `%LOCALAPPDATA%\aspnet-review-l3\mirror.git`, outside the worktree. Override it with `--mirror <bare-path>`; this option is required if `LOCALAPPDATA` is unset. If a SHA is unavailable, the command exits nonzero and reports it as `unreachable`; the verified subset remains pinned. Omit a role only if its SHA was not recorded. Use a single writer when measuring mirror size.
- `outcomes` keeps only IDs, decisions, selectors, and 16-character SHA-256 prefixes, not source evidence. Worker candidate sections, verifier decisions, requested inline comments, and published comments are separate records. JSON selectors use JSON Pointers; lines are one-based and embedded transcript turn indexes are zero-based, or `null` when a result has no `[Turn n]` headers and its whole content is one turn. JSON item hashes cover `JSON.stringify(item)`; JSONL hashes cover the original line without its terminator. Prompt selectors hash the selected lines joined with LF, without a trailing terminator.
- `recorded` means an observation was located, not that every observation survived. `absent-in-evidence` does not mean the event never occurred. Missing artifact listings are `unknown` rather than presumed expired. The run-scoped artifact API cannot bind downloads reliably across reruns, so rerun artifact fields are `unknown`; attempt-specific job logs can still supply identity. If the agent job log cannot be read, `agentLog` reports `fetch-failed` with the `gh` exit code, the command warns on stderr, and log-derived commits are `unknown`.
- Action artifacts and logs can expire. Retain the mirror separately from the index. Neither the pinned trees nor the index reconstruct the exact hosted prompt, API responses, or missing verifier decisions.
