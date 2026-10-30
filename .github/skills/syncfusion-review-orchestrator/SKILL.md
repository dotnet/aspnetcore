---
name: syncfusion-review-orchestrator
description: >-
  Drive the durable Syncfusion pull-request review campaign from a repository-owned,
  fail-closed state machine. USE FOR resuming, simulating, migrating, or operating the
  Syncfusion G/M/S/O review queue. DO NOT USE FOR ad hoc PR review or direct GitHub writes.
---

# Syncfusion review orchestrator

This skill is the app adapter for the deterministic controller in `scripts/`.
The PowerShell state machine owns ordering, idempotency, evidence requirements, and
transition validity. Copilot app tools remain necessary for visible session creation,
nesting, model selection, messaging, inspection, and archival; repository scripts
cannot call those APIs.

## Hard boundaries

- Never operate against the historical live campaign ledger in place. Import it
  read-only, inspect the candidate, and explicitly accept a copied candidate ledger.
- Never execute more than one reserved intent per controller turn.
- Execute a `campaign-root` intent only in the campaign root session.
- Execute a `pr-coordinator` intent only inside the exact coordinator session recorded
  for that PR. Because `create_session` nests under its caller, this is required for
  G/M/S/O parentage.
- Never create G/M/S/O from the campaign root.
- Never replace or silently rerun an original arm. Unknown effects reconcile before
  retry; unresolved unknowns remain blocked.
- Never post a review, comment, label, approval, change request, resolution, or merge
  outside an accepted controller intent.
- Never use Haiku as a review arm. It is used only for launch-plan and finding
  falsification consultations.
- Let the frozen `review-pull-request` skill control build, test, run, and browser
  validation. Do not impose a source-only restriction.

## Operator loop

1. Ask the controller for the next intent:

   ```powershell
   pwsh .github/skills/syncfusion-review-orchestrator/scripts/Invoke-SyncfusionReviewOrchestrator.ps1 `
     -Command Next `
     -LedgerPath <copied-ledger.jsonl>
   ```

2. If the returned intent has `actor: campaign-root`, execute it here. If it has
   `actor: pr-coordinator`, send the exact intent to the recorded coordinator and let
   that session execute it.
3. Capture the tool/API result and independent observation in a receipt matching
   `references/receipt-contract.md`.
4. Apply the receipt:

   ```powershell
   pwsh .github/skills/syncfusion-review-orchestrator/scripts/Invoke-SyncfusionReviewOrchestrator.ps1 `
     -Command Receipt `
     -LedgerPath <copied-ledger.jsonl> `
     -ReceiptPath <receipt.json>
   ```

5. Repeat only after the receipt is accepted.

Pending intents are never reissued. `Next` returns the original action with
`reconcile: true` until the effect is independently observed. Record
`status: absent` only with proof that no effect occurred; the controller then creates
a new numbered attempt. `unknown` remains blocked on reconciliation.

## App-only intent execution

- `launch-consultation`: run one read-only `general-purpose` task with the exact model.
- `create-coordinator`: the campaign root creates exactly one coordinated project
  session with the deterministic name and nonce from the intent.
- `create-arm`: only the PR coordinator creates the visible child session with the
  exact explicit model and zero-tool initialization prompt.
- `verify-arm`: inspect session metadata and session events. Prove the serving model,
  idle state, nonce, and zero tool calls before the review assignment.
- `assign-arm`: send the frozen assignment. The first review-turn tool must be native
  `skill("review-pull-request")`; every routed guide worker must be a fresh
  `general-purpose` background task using the arm model explicitly.
- `observe-arm`: require terminal review output and terminal/idle guide workers.
- `observe-hosted-review`: bind one unique terminal hosted run to the exact trigger,
  prove its complete output-ID delta, and establish the post-trigger feedback baseline.
- `prepare-review-bundle`: prepare the ready-v2 bundle only after hosted observation
  and bind `feedback.json` to that post-trigger baseline before any arm is created.
- `run-assessment`, `run-falsifier`, and `handoff`: retain exact result bodies and
  hashes.
- `cleanup-children`: archive only recorded terminal child sessions after evidence is
  retained.

An imported active coordinator may be adopted only when retained evidence proves that
no G/M/S/O arm was created and both pre-coordinator consultations were accepted.
Otherwise terminalize the imported epoch; never silently recreate an original.

Use `session_store_sql` or the app's session event API for independently observed
model/tool-order receipts. Every accepted receipt names retained proof artifacts and
their SHA-256 hashes; the controller rehashes those files before accepting the
receipt. Agent prose is not proof. If the required event evidence is unavailable,
record `unknown`; do not claim verification.

## GitHub adapter

GitHub actions are rendered and validated by
`Invoke-SyncfusionGitHubAdapter.ps1`. Its default is dry-run. Live execution requires
an accepted controller intent plus both `-Execute` and `-AllowLiveMutation`. The
adapter:

- checks the authenticated login;
- obtains two consecutive complete GitHub identity snapshots and requires equality;
- re-reads the PR head, base ref, base tip, complete diff, and effective feedback
  immediately before and after every mutation;
- deduplicates `/review` per campaign/PR/head using retained receipts and readback;
- submits findings through one `COMMENT` review pinned to the frozen commit;
- recomputes the complete live diff and feedback hashes, excluding only exact
  ledger-bound controller effect IDs, then prevalidates every RIGHT-side changed-line
  anchor and finding fingerprint;
- verifies the review, inline comments, marker, commit, and URL by readback; and
- applies the clean label/comment only for a complete all-`NO_FINDINGS` panel with
  zero assessment survivors.

No adapter operation approves, requests changes, resolves, or merges.

## Dry-run and canary

Dry-run reserves nothing and calls neither GitHub nor app APIs:

```powershell
pwsh .github/skills/syncfusion-review-orchestrator/scripts/Invoke-SyncfusionReviewOrchestrator.ps1 `
  -Command Simulate `
  -ConfigPath .github/skills/syncfusion-review-orchestrator/tests/fixtures/campaign.json
```

The deterministic canary runs the complete fixture suite, including a full PR
transition, import acceptance, crash reconciliation, adapter execution/readback, and
cleanup:

```powershell
pwsh .github/skills/syncfusion-review-orchestrator/scripts/Invoke-SyncfusionReviewOrchestrator.ps1 `
  -Command Canary
```

The current repair mode is dry-run only. Do not pass `-AllowLiveMutation` and do not
execute a live canary without fresh user approval.

See `references/architecture.md` for the architecture and migration boundary.
