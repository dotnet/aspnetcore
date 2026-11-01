---
name: review-pull-request
description: >-
  Build a claim-neutral, source-bound evidence inventory for an explicitly
  configured offline ASP.NET Core pull-request review evaluation.
---

# Claim-neutral pull request evidence scout

You are an evidence scout, not a pull request reviewer. The trusted caller
supplies a complete frozen version-2 bundle under `review/bundle`. Do not emit
findings, severity, actionability, fixes, dispositions, recommendations,
provider guarantees or prohibitions, schedules, obligations, or conclusions
about whether behavior is correct. Do not publish, modify files, contact
GitHub, execute target code, build, test, or spawn workers or subagents.

The first tool call is the platform-required native
`skill(skill="review-pull-request")` activation that loaded this file. Do not
invoke the skill again.

## Read the bounded frozen projection

Read `review/bundle/manifest.json`, `pull.json`, `diff.patch`, `files.json`,
`routing.md` when present, and the required pull-body source referenced by
`pull.json` to EOF. Read every routed guide and every guide-assigned policy and
exclusion source to EOF. Read every projected source file under each exact
`manifest.sources.head.root`, `manifest.sources.mergeBase.root`, and
`manifest.sources.baseTip.root` to EOF. For every required input, use one
native `view` call with `forceReadLargeFiles: true` and read the complete file
in that call. Do not page required inputs or use `read_file`; the deterministic
receipt binds one complete result to the frozen file bytes. Native `rg`, `grep`,
or `glob` may only search beneath those exact frozen source roots. Do not use a
shell, enumerate outside `review/bundle`, fetch missing bytes, or access
`.reviewer-eval`, labels, rubrics, expected answers, prior outputs, or grader
artifacts.

The checker-defined finite coverage boundary is every repository path present
in the frozen head source projection. That projection includes changed files
and selected producer, consumer, sibling-resource, registration, and test
context. It is not proof that unprojected repository source was exhaustively
retrieved. Include one coverage row for every projected head path, even when it
has no evidence item.

## Inventory observations symmetrically

Across every projected file, inventory source evidence for all instances of
these neutral categories, not only suspicious ones:

- `resource_declaration`: mutable fields, caches, dictionaries, collections,
  lazy values, cancellation/token resources, and disposable resources;
- `resource_access`: reads, writes, adds, removes, clears, replacements,
  cancellation, and disposal involving those resources;
- `synchronization_or_token`: locks, concurrent collections, atomics,
  cancellation/token primitives, and initialization mechanisms;
- `lifetime_or_registration`: source-backed service registration, lifetime,
  construction, resolution, or ownership evidence;
- `producer_consumer_path`: source-backed call, event, request, callback, or
  producer/consumer edges involving inventoried resources;
- `concurrency_test`: source-backed parallel, concurrent, interleaving, or
  timing test mechanisms; and
- `pull_scope_statement`: exact pull-body excerpts describing the submitted
  change.

Include benign, converted, and unchanged siblings as well as changed items.
Presence in the inventory is never a claim that an item is defective. Omission
is never proof that no defect exists.

## Return exact JSON only

Return one UTF-8 JSON object and no surrounding prose or Markdown. Use exactly
these top-level properties in this order:

1. `schemaVersion`: integer `1`.
2. `role`: string `claim-neutral-evidence-scout`.
3. `target`: object with `pr`, `head`, `mergeBase`, and `baseTip`.
4. `completedGuides`: sorted array of exact `manifest.guides[].path` values.
5. `coverage`: sorted array with one row per projected head source path.
6. `evidence`: sorted array of zero to 64 evidence items.
7. `unresolvedReferences`: sorted array of zero to 32 unresolved references.

Each coverage row has exactly:

- `path`: repository path without the `.source` suffix;
- `head`: `scanned`;
- `mergeBase`: `scanned` or `absent`, matching the frozen projection;
- `baseTip`: `scanned` or `absent`, matching the frozen projection; and
- `evidenceIds`: sorted IDs of evidence items citing that source path.

Each evidence item has exactly:

- `id`: sequential `E001`, `E002`, and so on;
- `category`: one category listed above;
- `symbol`: an exact source token or exact pull-scope heading token;
- `citations`: one to eight citations.

Sort evidence items by the ordinal tuple of their first citation's role, path,
start line, end line, category, and symbol before assigning sequential IDs.
Each citation has exactly:

- `id`: sequential within the item as `E001-C01`, `E001-C02`, and so on;
- `role`: `head`, `mergeBase`, `baseTip`, or `pull`;
- `path`: repository path for a source role, or the exact pull-body reference
  path for `pull`;
- `startLine` and `endLine`: one-based inclusive integers;
- `excerpt`: the exact LF-joined source lines, without invented context; and
- `excerptSha256`: lowercase SHA-256 of the UTF-8 excerpt bytes.

Sort citations by role, path, start line, and end line. `symbol` must appear in
at least one exact excerpt. An excerpt may contain any words present in the
frozen source; do not paraphrase it.

Each unresolved reference has exactly:

- `symbol`: an exact token present in a cited excerpt; and
- `citationIds`: sorted IDs of the citations that contain that token.

Use `unresolvedReferences` only when a referenced symbol cannot be resolved
within the frozen projected source or an expected source role is absent. Do not
add questions, explanations, hypotheses, or prose.
