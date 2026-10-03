# Narrow schema-evidence provider proof

> **Active evidence landing.** Return to the [product design](../design.md#narrow-runtime-enforced-evidence),
> [API review 4](../api-surface.md#review-4-narrow-evidence), or the
> [claim-to-proof index](README.md).

## Claim

`IOpenApiSchemaEvidenceProvider` can reveal a small fact that an identified
System.Text.Json converter or endpoint parser already enforces, and ASP.NET can
project that fact consistently into OpenAPI without installing a schema
validator.

## Why this needs proof

Evidence is intentionally narrower than a complete schema. The safety boundary
depends on the provider seeing the effective runtime mechanism, producing only
the closed evidence algebra, remaining inert when that mechanism is absent, and
failing explicitly for conflicting or invalid claims. Exact numeric evidence
must also survive OpenAPI serialization without fixed-precision loss.

## Setup

The repository tests use:

- `StrictIdConverter`, which accepts a converter-owned string language;
- `StrictIdSchemaEvidenceProvider`, which recognizes that converter and reports
  string shape, pattern, and format;
- `BoundedNumberConverter`, which parses an exact JSON number and enforces
  arbitrary-precision bounds;
- `BoundedNumberSchemaEvidenceProvider`, which reports those same exact bounds;
- positional tuple converters and evidence under both reflection and
  source-generated STJ metadata; and
- deliberately conflicting, repeated, invalid, and throwing providers.

No runtime JSON Schema validator is registered. The converter remains runtime
enforcement; evidence changes OpenAPI only.

## Success criteria

The proof passes when:

1. evidence is emitted only when the matching effective converter/parser is
   present;
2. the provider receives declared/effective type, `JsonTypeInfo`, converter, and
   request/response purpose;
3. OpenAPI 3.0, 3.1, and 3.2 preserve the reported shape and exact numeric
   bounds;
4. reflection and source-generated metadata produce the same positional shape;
5. invalid evidence, competing claims, and provider exceptions fail explicitly;
   and
6. exact numbers normalize, compare, format, and reject pathological input
   without decimal/double truncation.

## What is covered

| Concern | Evidence |
| --- | --- |
| Converter provenance and direction | `SchemaEvidenceProvider_EmitsConverterBackedScalarEvidence` asserts the effective converter and output purpose in provider context. |
| Cross-version emission | The converter-backed scalar and exact-number tests run for OpenAPI 3.0, 3.1, and 3.2. |
| Exact numeric output | `SchemaEvidenceProvider_EmitsExactArbitraryPrecisionNumericBounds` asserts the typed model and serialized JSON tokens, including a large fractional minimum and `1e+1000` maximum. |
| Format policy | `SchemaEvidenceProvider_FormatObeysScalarFormatPolicy` and `SchemaEvidenceProvider_FormatCallbackReceivesPolicyFilteredCandidate` preserve pattern while applying framework format policy. |
| Positional metadata | `SchemaEvidenceProvider_PreservesTupleShapeAcrossReflectionAndSourceGeneratedMetadata` compares reflection and source-generated discovery. |
| Defensive copies and closed algebra | `SchemaEvidenceResults_AreValidatedAndPositionalElementsAreCopied` rejects invalid kind/property combinations and reversed bounds, then proves copied positional elements are immutable from caller mutation. |

## Headline result

The tests demonstrate the intended proof boundary: a recognized enforcing
converter produces stronger OpenAPI; the same CLR type without that converter
does not. Reported exact numbers reach the serialized OpenAPI document without
precision loss, while format policy remains framework-controlled.

## Safety and failure cases

| Case | Expected result |
| --- | --- |
| Provider registered but enforcing converter absent | `SchemaEvidenceProvider_WithoutEnforcingConverterIsInert` leaves the ordinary object schema unchanged. |
| Two providers claim the same shape | `SchemaEvidenceProviders_RunInRegistrationOrderAndRejectMultipleClaims` proves ordinal registration order and rejects ambiguity. |
| Same provider instance registered twice | `SchemaEvidenceProvider_SameInstanceIsEvaluatedOnce` prevents duplicate evaluation. |
| Provider throws | `SchemaEvidenceProviderExceptionsPropagate` preserves the provider exception rather than returning a success-shaped fallback. |
| Invalid evidence construction | Constructor tests reject unknown kinds, incompatible pattern/bound combinations, reversed bounds, and mutable positional input. |
| Invalid or excessive JSON number | Number tests reject non-JSON syntax, unrepresentable exponents, and inputs/significands over the 10,000-character/digit limit. |

## Exact tests

The provider and OpenAPI-emission tests are in
[`OpenApiSchemaService.TupleSchemas.cs`](../../../src/OpenApi/test/Microsoft.AspNetCore.OpenApi.Tests/Services/OpenApiSchemaService/OpenApiSchemaService.TupleSchemas.cs):

- `SchemaEvidenceProvider_PreservesTupleShapeAcrossReflectionAndSourceGeneratedMetadata`
- `SchemaEvidenceResults_AreValidatedAndPositionalElementsAreCopied`
- `SchemaEvidenceProvider_EmitsConverterBackedScalarEvidence`
- `SchemaEvidenceProvider_EmitsExactArbitraryPrecisionNumericBounds`
- `SchemaEvidenceProvider_FormatObeysScalarFormatPolicy`
- `SchemaEvidenceProvider_FormatCallbackReceivesPolicyFilteredCandidate`
- `SchemaEvidenceProvider_WithoutEnforcingConverterIsInert`
- `SchemaEvidenceProviders_RunInRegistrationOrderAndRejectMultipleClaims`
- `SchemaEvidenceProvider_SameInstanceIsEvaluatedOnce`
- `SchemaEvidenceProviderExceptionsPropagate`

Exact number syntax, normalization, ordering, conversions, resource limits, and
evidence-bound validation are in
[`OpenApiSchemaNumberTests.cs`](../../../src/OpenApi/test/Microsoft.AspNetCore.OpenApi.Tests/OpenApiSchemaNumberTests.cs).

## Traceability

| Field | Source or result |
| --- | --- |
| Product code under test | [`OpenApiSchemaEvidenceResolver.Resolve`](../../../src/OpenApi/src/Services/Schemas/Inference/OpenApiSchemaEvidenceResolver.cs), evidence and exact-number public types in [`OpenApiSchemaEvidence.cs`](../../../src/OpenApi/src/Services/Schemas/OpenApiSchemaEvidence.cs), [`InferredScalarContractFactBuilder.Build`](../../../src/OpenApi/src/Services/Schemas/Inference/InferredScalarContract.cs), and emission in [`OpenApiSchemaService`](../../../src/OpenApi/src/Services/Schemas/OpenApiSchemaService.cs) |
| Producer/harness code | [`OpenApiSchemaService.TupleSchemas.cs`](../../../src/OpenApi/test/Microsoft.AspNetCore.OpenApi.Tests/Services/OpenApiSchemaService/OpenApiSchemaService.TupleSchemas.cs) and [`OpenApiSchemaNumberTests.cs`](../../../src/OpenApi/test/Microsoft.AspNetCore.OpenApi.Tests/OpenApiSchemaNumberTests.cs) |
| Exact command | Activate the repository SDK, then run the built xUnit assembly with `-method "OpenApiSchemaServiceTests.SchemaEvidence*"` and separately `-class OpenApiSchemaNumberTests`; the complete repository command is `source activate.sh && ./src/OpenApi/build.sh -test` |
| Retained output | [`schema-evidence-focused-current.txt`](schema-evidence-focused-current.txt) and the complete [`validation-current.txt`](validation-current.txt) |
| Result mapping | Provider selection: 16/16 discovered cases passed. Exact-number selection: 34/34 discovered theory rows passed. The stable methods listed under [Exact tests](#exact-tests) map each claim to its assertion. |

## Limitations

- This is behavioral test evidence, not an evidence-provider performance
  benchmark. No independent benchmark claim is made.
- The provider algebra covers strict scalar and fixed positional-array facts, not
  arbitrary JSON Schema objects, composition, vocabularies, or business rules.
- Evidence cannot make a permissive converter strict. The provider author is
  responsible for recognizing the exact runtime mechanism that proves the fact.
- Schema success does not replace or guarantee STJ/binder success. Runtime
  enforcement remains the converter/parser.

## Return to proposal and API

- [Design: narrow runtime-enforced evidence](../design.md#narrow-runtime-enforced-evidence)
- [Architecture: narrow evidence providers](../architecture.md#narrow-evidence-providers)
- [API review 4](../api-surface.md#review-4-narrow-evidence)
- [Evidence index](README.md)
