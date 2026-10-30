# Architecture and automation boundary

## Chosen architecture

The executable surface is a hybrid controller:

| Layer | Responsibility | Mutation capability |
|---|---|---|
| PowerShell state machine | Queue order, state validation, transition planning, exact action keys, hash-chained ledger, receipt validation, resume, cleanup gates | Local copied ledger only |
| GitHub REST adapter | Readback, exact-head checks, `/review` dedupe, atomic `COMMENT` review publication, clean marker/label idempotency | GitHub only with explicit `-Execute` |
| Copilot custom agent/skill | Visible session creation, nesting, explicit model selection, messaging, status/event inspection, child archival | App-only APIs |
| Optional session automation | Wake the same campaign root and execute one controller intent | No authority beyond the controller |

Repository scripts cannot call Copilot app session APIs. Dynamic Workflows are
session-scoped, experimental, and create hidden workflow-owned subagents. They do not
provide the required visible root -> PR coordinator -> G/M/S/O hierarchy, and nested
workflows are unsupported. A GitHub Agentic Workflow similarly cannot create or own
the required local app sessions. Neither is used as campaign authority.

## Durable invariants

- The configured queue has exactly 33 unique ordered PR entries.
- At most one PR is active.
- Gemini 4 Argon and Haiku 5.5 launch-plan receipts precede coordinator creation.
- Exactly one coordinator session is accepted per PR epoch.
- Only the recorded coordinator may create its G/M/S/O child sessions.
- Arms are attempted once, strictly G -> M -> S -> O, with exact models.
- A pending side effect is reconciled before any retry.
- A head change terminates the frozen epoch; it never silently advances the cohort.
- `/review` is keyed by campaign, PR, and exact head.
- Arms cannot start until the exact hosted run is terminal, its output delta is
  attributed to the trigger, and a new ready-v2 bundle is prepared from the resulting
  feedback baseline.
- A clean publication requires four valid `NO_FINDINGS` originals and zero assessment
  survivors. A discarded `FINDINGS` result is not clean-panel evidence.
- Finding publication requires complete feedback, exact live head, valid changed-line
  anchors, hosted/local dedupe, one `COMMENT` review, and verified readback.
- The next PR cannot activate until the coordinator handoff is retained, every child
  is archived, and the coordinator is archived.

## Ledger

The JSONL ledger is append-only. Every event has:

- a monotonically increasing sequence;
- the prior event hash;
- a canonical JSON SHA-256 hash;
- a stable campaign, PR/head, action, and intent identity where applicable; and
- the exact validated receipt or transition payload.

The snapshot is derived by replay and is never authoritative. A partial final line,
sequence gap, prior-hash mismatch, event-hash mismatch, stale expected revision, or
unrecognized event type halts replay.

The frozen `diffSha256` and `feedbackSha256` use the GitHub adapter's canonical
projections. Feedback is frozen before the hosted trigger. Later gates exclude only
the exact ledger-bound `/review` comment ID; identical text from another actor remains
feedback. Recovery subtracts only exact immutable IDs created by the controller effect
being reconciled. Before and after every effect the adapter obtains two consecutive
complete snapshots of PR state, base ref/tip, files, top-level issue comments,
submitted reviews, and inline review comments. The snapshots must match, the unique
file count must equal GitHub's `changed_files`, IDs and paths must be unique, and the
effective hashes must equal the frozen values.

Native skill identity is not the whole `SKILL.md`. The runtime removes the first exact
YAML frontmatter block and exactly one separator blank line after strict UTF-8 and
CRLF-to-LF normalization. The controller records the normalized whole-file SHA-256 and
the runtime-delivered body SHA-256 under transform
`yaml-frontmatter-stripped-crlf-to-lf-v1`, while separately verifying the reviewer
commit/path Git blob.

## Migration

1. Copy the legacy control JSON and marching-orders document to a read-only snapshot
   location. Do not point the controller at the active campaign files.
2. If legacy facts are stored in SQLite, create a consistent read-only export with
   `Export-LegacySqlite.py`. It opens the source with `mode=ro`, uses SQLite backup
   into an in-memory snapshot so WAL frames are included, exports only named tables,
   and verifies the database/WAL/SHM hashes did not change.
3. Run `Import-SyncfusionReviewCampaign.ps1` against the copied control JSON, optional
   marching orders, and optional SQLite export. It validates 33 unique queue entries,
   cross-checks every queued PR against the Markdown when supplied, validates the
   SQLite export envelope, preserves legacy completion/active evidence as imported
   facts, and writes a candidate campaign configuration plus source hashes.
4. Review the candidate order hash and active/completed mapping.
5. Initialize a new ledger from the candidate. Candidate metadata automatically
   requires an explicit `accept-import` receipt.
6. Imported completed PRs become terminal facts. An imported active PR produces a
   dedicated reconciliation intent; it must be adopted with verified parent/session
   evidence or terminalized before any replacement coordinator is created.
7. Treat any mid-flight legacy arm without independently verifiable model/tool-order
   evidence as incomplete historical evidence. Do not synthesize a valid original or
   rerun it silently.

The import scripts never modify the source Markdown, JSON, SQLite database, WAL, or
SHM files.

## Cutover recommendation

Keep the current manual queue authoritative until:

1. the implementation branch is merged or otherwise frozen at a reviewed commit;
2. import produces the expected 33-entry order hash;
3. one fixture canary and one user-approved non-active PR canary complete with exact
   receipts and zero duplicate app/GitHub effects; and
4. the parent session automation is changed to wake this custom agent and execute one
   reserved intent per tick.

Do not cut over in the middle of an active PR epoch.
