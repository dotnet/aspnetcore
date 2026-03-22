# Triage Schema Quick Reference

Read this BEFORE generating JSON. Full schema: `references/triage-schema.json`.

## Required Top-Level Fields

| Field | Type | Required Sub-fields |
|-------|------|-------------------|
| `meta` | object | `schemaVersion` ("1.0"), `number` (int), `repo` ("dotnet/aspnetcore"), `analyzedAt` (ISO 8601) |
| `summary` | string | — (one-sentence factual description) |
| `classification` | object | `type` + `area` (see below) |
| `evidence` | object | — (no sub-fields required, include what's relevant) |
| `analysis` | object | `summary` (string, required) |
| `output` | object | `actionability` + `actions` (see below) |

## Classification (required sub-objects)

```json
"classification": {
  "type": { "value": "<classifiedType>", "confidence": 0.0-1.0 },
  "area": { "value": "<classifiedArea>", "confidence": 0.0-1.0 },
  "feature": { "value": "<classifiedFeature>", "confidence": 0.0-1.0 },  // optional
  "platforms": ["os/Windows", ...],   // optional, plain string array NOT objects
  "tenets": ["tenet/performance", ...] // optional, plain string array NOT objects
}
```

⚠️ `platforms` and `tenets` are **plain string arrays** — NOT `{value, confidence}` objects.

## Evidence & Analysis Fields

| Field | Structure / Notes |
|-------|-------------------|
| `evidence.bugSignals` | `{ severity, regressionClaimed, errorType, errorMessage, stackTrace, reproQuality, dotnetVersion, aspnetcoreVersion, targetFrameworks }`. Include ONLY for bugs. |
| `evidence.reproEvidence` | Extract ALL screenshots, attachments, code snippets, steps. Preserve every URL. |
| `evidence.versionAnalysis` | `{ mentionedVersions[], workedIn, brokeIn, currentRelevance, relevanceReason }`. Include when version info is mentioned. |
| `evidence.regression` | `{ isRegression, confidence, reason, workedInVersion, brokeInVersion }`. Include if regression signals exist. |
| `evidence.fixStatus` | `{ likelyFixed, confidence, reason, relatedPRs, relatedCommits, fixedInVersion }`. Include if fix evidence exists. |
| `analysis.codeInvestigation` | Array of `{ file, finding, relevance, lines? }`. **MANDATORY for bugs**: at least one entry. |
| `analysis.keySignals` | Array of `{ text, source, interpretation? }`. Structured evidence quotes. |
| `analysis.rationale` | Single string summarizing key classification decisions. |
| `analysis.workarounds` | Array of workaround strings found during triage. |
| `analysis.nextQuestions` | Array of open questions for repro/fix to investigate. |
| `analysis.errorFingerprint` | Normalized error fingerprint for cross-issue dedup (optional). |
| `analysis.resolution` | `{ hypothesis?, proposals[], recommendedProposal, recommendedReason }`. Omit for duplicates/abandoned. |

## Enum Values

| Enum | Values |
|------|--------|
| **classifiedType** | `bug`, `enhancement`, `feature-request`, `question`, `documentation` |
| **classifiedArea** | `area-auth`, `area-blazor`, `area-commandlinetools`, `area-dataprotection`, `area-grpc`, `area-healthchecks`, `area-hosting`, `area-identity`, `area-infrastructure`, `area-middleware`, `area-minimal`, `area-mvc`, `area-networking`, `area-perf`, `area-routing`, `area-security`, `area-signalr`, `area-ui-rendering` |
| **classifiedPlatform** | `os/Windows`, `os/Linux`, `os/macOS`, `os/WASM` |
| **classifiedTenet** | `tenet/compatibility`, `tenet/performance`, `tenet/reliability`, `tenet/security` |
| **suggestedAction** | `needs-investigation`, `close-as-fixed`, `close-as-by-design`, `close-with-docs`, `close-as-duplicate`, `convert-to-discussion`, `request-info`, `keep-open` |
| **errorType** | `crash`, `exception`, `wrong-output`, `missing-output`, `performance`, `build-error`, `memory-leak`, `security`, `platform-specific`, `other` |
| **severity** | `critical`, `high`, `medium`, `low` |
| **reproQuality** | `complete`, `partial`, `none` |
| **currentRelevance** | `likely`, `unlikely`, `unknown` |
| **relevance** (codeInvestigation) | `direct`, `related`, `context` |
| **category** (proposals) | `workaround`, `fix`, `alternative`, `investigation` |
| **validated** (proposals) | `untested`, `yes`, `no` |
| **actionType** | `update-labels`, `add-comment`, `close-issue`, `convert-to-discussion`, `link-related`, `link-duplicate`, `update-project`, `set-milestone` |

## Output (required)

```json
"output": {
  "actionability": {
    "suggestedAction": "<suggestedAction enum>",
    "confidence": 0.0-1.0,
    "reason": "Why this action"
  },
  "missingInfo": ["What info is needed from reporter"],
  "actions": [
    { "type": "<actionType>", "description": "...", "risk": "low|medium|high", "confidence": 0.0-1.0, ... }
  ]
}
```

### Action Types & Specific Fields

| Type | Risk | Required Specific Fields |
|------|------|--------------------------|
| `update-labels` | low | `labels` (array of strings) |
| `add-comment` | high | `comment` (markdown string). See `response-guidelines.md`. |
| `close-issue` | medium | — |
| `link-related` | low | `linkedIssue` (integer) |
| `link-duplicate` | medium | `linkedIssue` (integer) |
| `convert-to-discussion` | high | — |
| `update-project` | low | — |
| `set-milestone` | low | — |

## Common Mistakes

1. **`platforms`/`tenets` as objects** — They're plain string arrays, NOT `{value, confidence}`
2. **Missing `meta.analyzedAt`** — Must be ISO 8601: `"2026-01-15T12:00:00Z"`
3. **Wrong `meta.repo`** — Must be `"dotnet/aspnetcore"` exactly
4. **Extra fields** — `additionalProperties: false` at every level. No extra keys.
5. **Null values** — Omit optional fields entirely. Never set to `null`.
6. **Absolute paths** — Redact `/Users/name/` → `$HOME/`
7. **`bugSignals` without `bug` type** — Only include when `classification.type.value` is `"bug"`.
8. **Invalid area** — Use only the 18 `area-*` values. Do NOT invent new ones. Use dedicated labels: `area-routing` (not `area-mvc`) for routing, `area-middleware` for middleware, `area-minimal` for Minimal APIs, `area-networking` for Kestrel/HTTP.
