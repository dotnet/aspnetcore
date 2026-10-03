# Runtime validated-schema adapter proof

> **Active evidence landing.** Return to
> [Architecture: runtime registration](../../architecture.md#runtime-registration),
> [Design: outputs and runtime behavior](../../design.md#outputs-and-runtime-behavior),
> or the [evidence index](../README.md).

## Claim

ASP.NET's runtime validator factory can compile one reusable validator during
registration, then apply an engine-neutral directional endpoint plan to request
and response UTF-8 without making either validation engine responsible for HTTP
policy.

## What was exercised

The non-shipping evidence app registers immutable Draft 4, 6, 7, 2019-09, and
2020-12 schemas through two private adapters:

- Corvus validates raw UTF-8 and supports the listed dialects;
- JsonSchema.Net validates through a private registry and supports Draft 6 and
  later; Draft 4 is rejected before compilation.

The external engines demonstrate replaceability only. The shipping
`Microsoft.AspNetCore.OpenApi` project references neither package.

Each `IOpenApiJsonSchemaValidatorFactory.CreateValidator` call executes once
during registration. The resulting thread-safe
`IOpenApiJsonSchemaValidator` is reused concurrently. No request fetches remote
references or recompiles schema.

## Success criteria

1. Both adapters agree on the equivalent valid/invalid recursive corpus for
   supported dialects.
2. Semantic identity separates source schema, declared dialect/capabilities,
   and acceptance-affecting engine configuration.
3. The real endpoint wrapper produces valid pass-through, invalid-request 400,
   oversized-request 413, invalid-response suppression/500, and correct
   annotation-only versus asserted-format behavior.
4. The standalone executable proves Minimal API policy; repository MVC tests
   separately prove that MVC uses the same neutral result and endpoint plan.
5. Repeated and concurrent validation reuses the compiled validator.

## Headline result

The recorded executable met the engine and Minimal API criteria. The repository
MVC tests met the MVC criterion. Unsupported dialects, unresolved or
non-local references, and invalid registrations fail before validator creation.
Framework errors remain engine-neutral and client-safe. Runtime compilation is
one-time registration work; request/response execution uses the cached validator
and precomputed endpoint plan.

## Framework proof versus engine proof

| Boundary | What the evidence demonstrates | What it does not assign to the engine |
| --- | --- | --- |
| Factory/validator | Dialect support, one-time compile, reusable raw-UTF8 execution, native-to-neutral result mapping | Endpoint selection, buffering, payload limits, status/content-type policy |
| Endpoint plan | Request rewind/bind after validation, response capture/suppress, 400/413/500 behavior, Minimal/MVC parity | Native schema semantics or diagnostics wording |
| Identity/cache | Different schema/dialect/capabilities/configuration cannot collide | OpenAPI target version does not redefine schema authority |

The executable's `RunMinimalApiEvidenceAsync` method is intentionally Minimal
API only. MVC proof comes from
[`ValidatedJsonSchemaTests.cs`](../../../../src/OpenApi/test/Microsoft.AspNetCore.OpenApi.Tests/ValidatedJsonSchemaTests.cs),
including `MvcConvention_InvalidRequestStopsBeforeInputFormattingAndReturns400`,
`MvcConvention_ValidRequestIsRewoundForInputFormatter`,
`MvcConvention_ResultFilterAndOutputFormatterAreValidatedBeforeWriting`,
`MvcConvention_InvalidFormattedResponseIsSuppressedAndReturns500`,
`MvcConvention_SelectsActualStatusCodeAndContentType`, and
`MvcConvention_EnforcesRequestAndResponseLimits`.

## Traceability

| Field | Source or result |
| --- | --- |
| Product code under test | Factory/evidence contracts in [`OpenApiSchemaEvidence.cs`](../../../../src/OpenApi/src/Services/Schemas/OpenApiSchemaEvidence.cs) and `OpenApiValidatedJsonSchemaEndpointPlan`/`WithValidatedJsonSchema` in [`OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions.cs`](../../../../src/OpenApi/src/Extensions/OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions.cs) |
| Producer/harness code | [`Program.cs`](Program.cs), [`CorvusValidatorFactory.cs`](CorvusValidatorFactory.cs), [`JsonSchemaNetValidatorFactory.cs`](JsonSchemaNetValidatorFactory.cs), and MVC methods in [`ValidatedJsonSchemaTests.cs`](../../../../src/OpenApi/test/Microsoft.AspNetCore.OpenApi.Tests/ValidatedJsonSchemaTests.cs) |
| Exact command | `source activate.sh && dotnet run --project docs/OpenApiInferenceProposal/evidence/validated-schema-adapters/ValidatedSchemaAdapters.csproj --no-build`; repository MVC coverage runs under `source activate.sh && ./src/OpenApi/build.sh -test` |
| Retained output | Sanitized [`adapter-execution-current.txt`](adapter-execution-current.txt) and repository [`validation-current.txt`](../validation-current.txt) |
| Result mapping | `RunCorpusEvidence`, cache/concurrency and format checks map engine claims; `RunMinimalApiEvidenceAsync` maps 200/400/413/500; the named MVC tests map formatter, rewind, selection, limit, and suppression behavior |

## Performance and allocation

The [runtime allocation interpretation](allocation-results.md) separately
defines warmed framework-wrapper, MVC, valid-engine, and invalid-diagnostic
operations. It gives the design constraint, comparator, included/excluded work,
method, units, acceptance criterion, result, and caveat before linking raw BDN
reports.

## Limitations

- This app demonstrates runtime factory/validator integration, not generated
  binding or build-time compilation.
- Repository project-reference propagation prevents this app from isolating a
  meaningful trimmed publish: `PublishTrimmed` reaches source-generator and
  `net462` projects and fails with `NETSDK1124` before adapter analysis.
- It therefore makes no runtime-adapter NativeAOT claim. The separate
  [generated deployment proof](../generated-schema-artifacts/current-proof.md#deployment-and-footprint)
  applies only to the isolated generated Corvus consumer.
- Cross-engine equivalence is asserted at acceptance and framework-policy
  boundaries, not identical native diagnostic text.

## Reproduce

From the repository root after activating the repository SDK:

```console
source activate.sh
dotnet run --project docs/OpenApiInferenceProposal/evidence/validated-schema-adapters/ValidatedSchemaAdapters.csproj --no-build
```

See also the [runtime validator APIs](../../api-surface.md#review-5-runtime-complete-schema-validation)
and [evidence index](../README.md).
