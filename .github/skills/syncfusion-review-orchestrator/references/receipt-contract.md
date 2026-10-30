# Receipt contract

Every receipt is untrusted input until the controller validates it.

```json
{
  "schemaVersion": 1,
  "intentId": "intent identifier returned by Next",
  "actionId": "logical action identifier returned by Next",
  "kind": "exact action kind",
  "status": "accepted | absent | failed | timed-out | unknown",
  "actorSessionId": "session that executed the intent",
  "observedAtUtc": "RFC 3339 UTC timestamp",
  "evidence": {
    "proofs": [
      {
        "path": "retained raw observation artifact",
        "sha256": "64 lowercase hex characters"
      }
    ]
  }
}
```

`accepted` means the effect was independently observed and all kind-specific
preconditions passed. `absent` means reconciliation proved that no effect occurred and
permits a new numbered attempt. `failed` means an attempted effect definitively failed.
`timed-out` preserves the incomplete attempt and never authorizes a replacement.
`unknown` means the effect may have happened; the controller blocks and reconciles
instead of retrying.

All accepted evidence objects include retained raw proof paths and hashes. The
controller rehashes them before acceptance. At least one proof is a JSON envelope
binding `intentId`, `actionId`, `kind`, observation source, observed object IDs, and
the SHA-256 of the receipt claim set. Model, tool-order, hierarchy, GitHub mutation,
and readback claims must come from app/GitHub observations rather than agent summaries.

GitHub effect receipts retain complete stable pre/post identity snapshots, raw and
effective feedback hashes, and every exact excluded object ID. A dry-run response is a
plan, not a receipt, and never authorizes `-Execute`.
