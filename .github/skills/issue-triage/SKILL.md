---
name: issue-triage
description: >-
  Classify or triage a dotnet/aspnetcore GitHub issue by its single best area, existing
  issue-type preservation or Bug/Feature/Task recommendation, supported subtype,
  duplicate status, abstention, and triage-summary recommendation. USE FOR requests to
  classify an ASP.NET Core issue, recommend triage metadata, distinguish Bug from Task,
  identify the owning ASP.NET Core area, assess a supplied duplicate candidate, or draft
  a triage summary. DO NOT USE FOR implementing an issue, investigating or fixing a bug,
  reviewing a pull request, or generic repository work.
---

# Classify an ASP.NET Core issue

Apply this policy only to GitHub issues for `dotnet/aspnetcore`. Work from the issue title,
body, and any supplied repository evidence. Recommend classifications; do not turn a
classification request into implementation or investigation work.

For a full triage, decide:

1. Exactly one area, or an explicit area abstention.
2. One type action: preserve a trusted existing issue type, or recommend exactly one of
   `Bug`, `Feature`, or `Task` for an untyped issue.
3. At most one supported subtype, or no subtype.
4. Whether a supplied or verified issue is a duplicate, merely related, or unrelated.
5. Whether uncertainty requires abstention.
6. A semantic triage summary suitable for maintainer review.

## Area

Read [references/areas.md](references/areas.md) before choosing an area. It contains the
complete supported area set, ownership boundaries, and disambiguation rules.

Choose the single best match from issue evidence such as API and type names, source paths,
stack traces, packages, and described behavior. Never return a second area as a fallback.
If the best area is below roughly 40% confidence, abstain from the area decision and
briefly identify the missing evidence.

An issue can describe an ASP.NET Core symptom while establishing that the root cause and
required fix belong to another tool or repository. In that case, prefer the `external`
subtype and do not invent an ASP.NET Core area merely from the application in which the
symptom appeared.

## Type

Treat a supplied trusted current issue type as authoritative. When it is non-empty,
report that it must be preserved and recommend no replacement type. This includes
maintainer-created `Epic` issues and template- or automation-assigned `Bug`, `Feature`,
or `Task` issues.

Only for an untyped issue, recommend exactly one of:

| Type | Use when |
|---|---|
| `Bug` | Reproducible current behavior is broken or differs from intended design. A small or mechanical fix to broken shipped behavior is still a Bug; fix size does not make it a Task. |
| `Feature` | The requested product behavior does not exist yet, including an addition or enhancement to existing behavior. |
| `Task` | Bounded maintenance, documentation, test, infrastructure, or refactoring work where current behavior is not broken. A docs-only deliverable is `Task` plus the `docs` subtype. |

Never recommend assigning `Epic`. It remains valid maintainer-managed planning metadata
in the dotnet organization, but it is not an automated newly-opened-issue
classification. A broad or large single feature request remains a `Feature`;
implementation size alone does not make it an Epic.

A template signal can inform classification of an untyped issue when the content supports
it, but it never overrides a trusted current issue type.

Existing-type preservation affects only the type action. Continue area, subtype,
duplicate, abstention, and summary analysis normally.

## Supported subtype

Choose at most one subtype, and only from this list:

| Subtype | Use when |
|---|---|
| `by-design` | Reported behavior differs from the reporter's expectation but is the intended design. |
| `question` | The issue asks how to use the product, requests clarification, or describes expected behavior rather than a defect or new behavior. |
| `external` | The root cause and required fix belong to a component, tool, service, or repository that the ASP.NET Core team does not own directly. |
| `docs` | The requested deliverable is missing, incorrect, or updated documentation or guidance. Docs-only work is a `Task`, not a `Feature`. |
| `api-proposal` | The issue formally proposes adding or changing public API. |
| `test-failure` | The issue reports a CI or test-infrastructure failure. |
| `performance` | The issue reports a performance regression or requests a performance optimization. |

If none applies, return no subtype. Do not translate historical or tempting labels such as
`enhancement`, `accessibility`, `severity-*`, `help wanted`, feature-family labels, or
resolution labels into supported subtypes.

## Regression

For a `Bug`, identify a regression only when the issue gives evidence that behavior worked
in an older version and stopped working in a newer version. Preserve the versions exactly
as stated without characterizing their release status.

Record:

- the previously working version, when stated;
- the version where the behavior became broken, when stated; and
- the concrete behavior change.

If regression wording is present but versions are missing, state only what is known and
what version evidence is missing. Otherwise omit regression information.

## Duplicate decision

Compare technical substance, not shared keywords:

- Same component and same symptom or request: duplicate when confidence is high.
- Same component but a different problem: not a duplicate.
- Similar error in a different context: related at most, not a duplicate.

Verify any cited issue's existence and substance from supplied or actually retrieved
evidence. Never invent an issue number. When confidence is not high, say `related` or
`none found` instead of `duplicate`.

## Confidence and abstention

Make only evidence-backed decisions. Do not guess from a generic product name when the
owning API, source path, component, or behavior is unclear.

- Area below roughly 40% confidence: abstain from area classification.
- Insufficient evidence for a subtype: return no subtype.
- Insufficient evidence that two issues describe the same problem: do not call them
  duplicates.
- Never compensate for uncertainty by returning multiple areas, unsupported labels, or a
  broader type.

State the narrow reason for an abstention and the evidence that would resolve it.

## Triage summary contract

For a full triage, first give a compact decision record containing the recommended area
(or abstention), type action (`preserve <existing type>` or `recommend Bug`, `Feature`,
or `Task`), subtype (or none), and duplicate decision. Never represent a preserved
existing `Epic` as a recommendation to assign `Epic`. Then draft the summary with this
semantic shape:

```markdown
### Triage Summary

**Area:** `area-xyz` (brief evidence-based reason)
**Type:** `<existing issue type>` (preserved) | `Bug` | `Feature` | `Task` (brief evidence-based reason)

#### Regression Info
- **Previously working version:** ...
- **Broken since:** ...
- Brief description of the behavior change

#### Potential Duplicates
- #123 - Title (similarity: high/medium)

#### Notes
- Optional additive, verified information
```

Apply these shape rules:

- Omit `Regression Info` unless the issue supports a regression.
- Always include `Potential Duplicates`; use exactly `- _None found_` when no verified
  candidate survives.
- Omit `Notes` when there is no additive verified information.
- Keep subtype recommendations in the decision record, not in the summary body.
- Do not add sections for applied labels, verdicts, process commentary, or unsupported
  metadata.

Every summary claim must come from the issue, repository evidence, or a source actually
consulted. Do not speculate, editorialize about issue quality, construct security impact,
compare third-party infrastructure as a correctness argument, or characterize a .NET
version as preview, RC, stable, released, or unreleased. Notes may contain justified code
pointers, deterministic regression evidence, specific missing-reproduction requests, or
verified cross-references; they must not restate the issue body.
