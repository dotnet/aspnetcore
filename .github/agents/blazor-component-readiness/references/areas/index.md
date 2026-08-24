# Quality-area playbooks

The core checklist plus explicitly selected overlays are the requirement inventory. These
playbooks explain how to collect and classify evidence without redefining requirements.

| Requirement IDs | Playbook |
|---|---|
| `LP-*`, `PI-*` | `provenance-integrity.md` |
| `SEC-*` | `security-privacy.md` |
| `A11Y-*` | `accessibility.md` |
| `BEQ-*` | `blazor-runtime.md` |
| `TA-*`, `PERF-*` | `trim-performance.md` |
| `CI-*` | `ci-release.md` |
| `SUP-*` | `support-lifecycle.md` |
| Optional `SCF-*`, `AI-*` overlays | `conditional-families.md` |

For a complete core review, consult every core playbook. Load `conditional-families.md` only when
an overlay is selected. For a targeted follow-up, load only the affected playbook, the canonical
requirement source, and prior exact-snapshot evidence.

## Evidence precedence

When evidence conflicts, prefer:

1. the exact released artifact;
2. a deterministic consumer or browser probe against that artifact;
3. source at the exact package commit;
4. current default-branch source and public configuration;
5. workflow intent or documentation;
6. unsupported inference.

Lower-ranked evidence cannot override contradictory higher-ranked evidence.
