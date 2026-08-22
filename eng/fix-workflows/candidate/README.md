# Fix workflow candidate contract

These files define the non-discoverable candidate protocol shared by
`fix-challenge` and `fix-issue`:

- `candidate-contract.md`
- `candidate-protocol.md`
- `empirical-protocol.md`
- `output-contract.md`
- `packet-schema.md`

Both skills resolve these files from the active `dotnet/aspnetcore` checkout and
record their SHA-256 hashes. They use stock independent task/subagent contexts.
That separation is procedural: it withholds peer outputs and incumbent answers,
but it is not a filesystem, network, credential, or runtime-model security
boundary.

Do not make this directory discoverable as an Agent Skill. Do not add a nested
Copilot CLI, custom transport, mount, or sandbox to enforce candidate isolation.
If the host cannot launch the configured independent agent, stop with
`blocked on orchestration`. Save initial and correction responses under distinct
immutable paths; a correction never replaces the original response.
