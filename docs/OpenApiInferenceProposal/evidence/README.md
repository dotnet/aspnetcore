# Claim-to-proof evidence index

This directory contains current interpretation pages, reproducible assets, and
retained raw results. It is not the proposal entry point. Start with the
[one-page proposal](../README.md), [product design](../design.md), or
[architecture](../architecture.md), then use this index to test a specific
claim.

## How to read evidence

Active links lead to an interpretation page before any raw report. Each landing
states the claim, setup, success condition, observed result, limitations, and
route back to the proposal. Raw BenchmarkDotNet reports, CSV files, generated
artifacts, and command transcripts remain unchanged behind those landings.

Performance evidence uses this field order:

0. **Design constraint / why measured**
1. **Question/hypothesis**
2. **Measured operation**
3. **Baseline/comparator**
4. **Included work**
5. **Excluded work**
6. **Method/environment**
7. **Units**
8. **Acceptance criterion/budget**
9. **Observed result**
10. **Interpretation/permitted conclusion**
11. **Non-conclusion/caveat**

`B/op` means managed bytes allocated per logical benchmark operation after
setup. BenchmarkDotNet values are warmed per-operation means unless a landing
says otherwise. Fresh-process samples, publish sizes, and deterministic hashes
are different evidence classes and must not be compared with warmed means.
Where no product budget exists, the landing says so rather than inventing one.

## Traceability convention

Every active landing identifies five links in one place:

| Field | Meaning |
| --- | --- |
| Product code under test | Shipping or proposed ASP.NET source that owns the behavior |
| Producer/harness code | Test, demo, benchmark, or external producer that creates the observation |
| Exact command | Repository-relative command used to reproduce the observation |
| Retained output | Report, CSV, transcript, document, or generated artifact kept in this tree |
| Result mapping | Stable test/method/symbol names that connect the output row or assertion to the claim |

Paths are repository-relative and prose names stable symbols or methods instead of brittle line
anchors. A landing marked **Partial** states the unavailable external input explicitly; a raw report
without this chain is not an active proof entry point.

## Pipeline-stage claim coverage

| Pipeline stage | Claim | Proof landing | Success signal | Important limitation |
| --- | --- | --- | --- | --- |
| Contract inference | Opt-in inferred mode improves serializer/binder fidelity without new authoring | [Core inference proof](core-inference-details.md#focused-comparisons-and-practical-impact) | Exact documents parse; representative direction, composition, scalar, transport, tuple, and 3.0/3.1/3.2 checks pass | Conservative spot checks, not proof of every CLR/STJ contract |
| Opaque-contract evidence | Narrow evidence appears only when a recognized converter/parser proves it | [Schema-evidence provider proof](schema-evidence-providers.md) | Converter-backed facts emit across versions; absent converter is inert; conflicts and invalid evidence fail | Documentation only; converter/parser remains enforcement |
| Canonical schema authority | One generated canonical resource graph remains authority across consumers | [Generated-artifact proof](generated-schema-artifacts/current-proof.md#canonical-authority-and-correctness) | Native graph, canonical resources, derived image, OpenAPI, Minimal API, and MVC agree on one corpus and graph identity | One bounded annotated model and unpublished producer POC |
| Validator binding | JsonSchema.Net and Corvus use symmetric ASP.NET validator/binding seams over one artifact | [Symmetric binding proof](generated-schema-artifacts/current-proof.md#symmetric-aspnet-binding-execution) | Same valid/invalid decisions and OpenAPI output through the same framework contracts | Engine timing/allocation informs choice; it is not a universal ranking |
| Endpoint enforcement / OpenAPI | Runtime factories share one directional endpoint plan and neutral HTTP policy | [Runtime adapter proof](validated-schema-adapters/README.md) | Minimal API/MVC request 400, size 413, response suppression/500, and version projection behave consistently | Runtime adapter app does not claim isolated trim/NativeAOT |
| Determinism | Clean builds produce stable artifact, configuration, binding, and image identities | [Deterministic build proof](generated-schema-artifacts/current-proof.md#deterministic-build-and-identities) | The retained verified-package run produced byte-identical generated evidence and hashes | Current replay is Partial because the exact unpublished package is unavailable; correctness/reproducibility evidence, not performance |
| Framework/engine allocation | Enforcement can add 0 B/op in the warmed successful framework wrapper while engines retain separate costs | [Runtime allocation interpretation](validated-schema-adapters/allocation-results.md) and [generated allocation interpretation](generated-schema-artifacts/allocation-results.md) | Matching pass-through/no-op framework paths allocate equally; engine allocations are reported separately | Excludes server hosting and separates STJ/engine work according to each landing |
| Trim/AOT | A generated Corvus-only consumer trims and NativeAOT-publishes without native JsonSchema.Net graph use in consumer IL | [Deployment interpretation](generated-schema-artifacts/current-proof.md#deployment-and-footprint) | Both publishes build/run and inspection finds no native graph initialization/reference | Bounded probe; copied package closure and absolute sizes are separate concerns |
| Dialect projection | Canonical input projects deliberately to OpenAPI 3.0, 3.1, and 3.2 | [Projection evidence](generated-schema-artifacts/current-proof.md#canonical-authority-and-correctness) and [core documents](core-inference-details.md#artifacts-and-normalization) | Both bindings emit equal target-version output; 3.0 widens unsupported semantics | OpenAPI 3.0 is intentionally lossy |
| Reproduction | The annotated-model proof can be rebuilt and inspected | [Hands-on demo](generated-schema-artifacts/two-stage-annotated-demo/README.md) | The documented run ends with `verified` and produces the listed artifacts | Requires the verified unpublished producer package or rebuilding the bounded POC |

## Current proof sets

| Evidence set | Current role |
| --- | --- |
| [Core inference](core-inference-details.md) | Exact emitted documents, extraction rules, structural checks, and conservative boundaries |
| [Narrow evidence providers](schema-evidence-providers.md) | Converter/parser provenance, exact numeric evidence, ordering, conflicts, and failure behavior |
| [Runtime validated-schema adapters](validated-schema-adapters/README.md) | Dynamic factory/validator integration, engine-neutral endpoint policy, and runtime allocation interpretation |
| [Generated schema artifacts](generated-schema-artifacts/README.md) | Generated authority/binding overview and navigation |
| [Current generated-artifact proof](generated-schema-artifacts/current-proof.md) | Correctness, symmetric engines, determinism, performance interpretation, and deployment |
| [Annotated-model reproduction](generated-schema-artifacts/two-stage-annotated-demo/README.md) | Commands for generation, validation, publish, and benchmark reproduction |

## Traceability status

| Pipeline row | Grade | Reason |
| --- | --- | --- |
| Contract inference | Complete | Product inference, standalone producer, exact commands, documents/excerpts, validator transcript, and result mapping are linked. |
| Opaque-contract evidence | Complete | Resolver/types/emitter, exact repository tests, focused commands, and 16 + 34 passing cases are linked. |
| Canonical schema authority | Partial | ASP.NET consumers, harness, commands, retained artifacts, and results are complete; the bounded external producer implementation is represented only by package hash and upstream commit because it is unpublished. |
| Validator binding | Complete | Both private adapters, stable verification methods, shared artifact, raw matched reports, and outputs are linked. |
| Endpoint enforcement / OpenAPI | Complete | The standalone executable is correctly scoped to Minimal API; MVC is mapped separately to named repository tests and the exact repository command. |
| Determinism | Partial | Prior verified-package hashes are retained, but a current two-clean-build replay is blocked because the exact unpublished package hash is unavailable. |
| Framework/engine allocation | Complete | Every active figure maps to a named benchmark method and retained Markdown/CSV or interpreted raw report with exact operation boundaries. |
| Trim/AOT | Complete | Project/program/image source, exact publish/run/inspection commands, current footprint, dependency, metadata, symbol, and string output are linked. |
| Dialect projection | Complete | Product importer/generator and `VerifyOpenApiProjectionAsync` map canonical input to retained 3.0/3.1/3.2 equality checks. |
| Reproduction | Partial | All repository commands and outputs are linked, but a fresh full annotated-model run requires the unavailable exact unpublished producer package. |

## Detailed and historical records

The current landings link deeper only when implementation history, raw numbers,
or superseded alternatives are needed:

- [Detailed same-pass technical record](generated-schema-artifacts/same-pass-exporter-removal.md)
- [Generated-artifact archive](generated-schema-artifacts/archive/README.md)
- [Released-generator inspection](generated-schema-artifacts/jsonschema-net-generation-inspection/README.md)
- [Historical collectible exporter](generated-schema-artifacts/two-stage-annotated-demo/exporter/README.md)
- [Full proposal design history](../archive/2026-10-prototype-design-history/README.md)

The exact recorded repository validation counts and package/hardware provenance
remain in the owning proof pages. They are evidence snapshots, not claims about
the current checkout.

Current repository validation is retained in
[`validation-current.txt`](validation-current.txt): Build 3/3, source generators
41/41, and OpenAPI 1,447 passed plus 5 skipped, for 1,491 passed plus 5 skipped
overall.
