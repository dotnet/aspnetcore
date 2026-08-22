# Frozen neutral packet

Every candidate in one comparison receives byte-identical packet content. Keep
role and transport metadata in a separate invocation envelope so role-specific
prompts do not silently change the evidence.

## Packet fields

```text
schema_version
mode
repository
frozen_sha
problem
product_oracle
oracle_authority
evidence_manifest
impact_map
target_files
validation
current_fix
prior_attempts
assertion_contract
allowed_perturbations
comparison_contract
contract_hashes
packet_sha256
```

Use `null` for `current_fix` in `candidate-propose`. Exclude candidate IDs,
configured models, role focus, peer outputs, incumbent answers, known fix PRs,
selection criteria, and evaluation answer keys.

## Invocation envelope

```text
candidate_id
configured_model
role
role_focus
voting
nonce
packet_path
packet_sha256
response_path
```

The orchestrator verifies the packet hash before launch, launches all read-only
candidates in one parallel turn, and saves each raw response unchanged. Separate
agent contexts and withheld peer outputs are procedural independence only.
