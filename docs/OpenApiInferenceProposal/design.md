# Design proposal: serializer-faithful OpenAPI schema inference

Supporting documents:

- [Benefits and proposal framing](benefits.md)
- [Architecture and extensibility](architecture.md)
- [Proposed API surface and review sequencing](api-surface.md)

## Summary

Add an opt-in, experimental OpenAPI schema-generation mode that derives the strongest stable schema justified by ASP.NET Core's effective System.Text.Json and Minimal API parameter-binding contracts. Keep the existing generation behavior as the default while gathering feedback on proof-based composition, deterministic identities, directional request/response schemas, converter-proven scalar constraints, and binder-aware parameter schemas.

The primary feature requires no new schema annotations, model style, custom converters, or provider registration. It improves documents for the same application code developers write today by interpreting the effective serializer and binding contracts more faithfully. An optional schema-evidence provider tier exists only for third-party runtime mechanisms whose enforced wire contract would otherwise be opaque; it strengthens specific proven facts and is neither the default inference architecture nor a prerequisite for inferred mode.

A further optional validated-schema tier supports applications that already possess a self-contained JSON Schema and a matching validator. It is separately reviewable: endpoint registration couples immutable schema identity, compile-once validation, and bounded request/response enforcement. It does not add third-party dependencies to ASP.NET Core, replace zero-authoring inference, or turn transformers into runtime validators.

## Motivation and goals

ASP.NET Core's current OpenAPI schema path provides broad type coverage by exporting a System.Text.Json schema and translating it into OpenAPI. That is a useful baseline, but it cannot always represent relationships or distinctions that matter to validators and generated clients:

- inheritance and polymorphism may be flattened or expressed without a durable composition model;
- `oneOf` can only be selected safely when alternatives are proven exclusive under effective serializer options;
- component names and late transformer-requested schemas need document-wide deterministic identity;
- request and response contracts can differ because setters, constructors, getters, requiredness, omission, and nullability are directional;
- CLR identity alone is not proof of a custom converter's JSON representation;
- familiar scalar formats can narrow the runtime contract, while exact integral bounds and base64 semantics may be lost;
- route, query, header, and form values are parsed from HTTP text and should not reuse JSON-body contracts.

The goal is not to infer every invariant from C# syntax. It is to introduce a conservative architecture that:

- treats effective runtime contracts as authoritative;
- separates immutable facts from typed OpenAPI decisions;
- emits stronger schemas only when evidence proves them;
- plans stable component identities across the whole document;
- supports OpenAPI 3.0, 3.1, and 3.2 deliberately;
- preserves transformer extensibility;
- has credible trimming, source-generation, and NativeAOT behavior;
- leaves unknown converter, parser, and business-rule behavior broad;
- preserves existing applications by remaining opt-in.

An implementation branch exercises the proposal end to end and provides concrete compatibility and validation evidence. A companion draft pull request would be opened solely as an executable design illustration: it is **not intended to be merged**, does not ask maintainers to review it as a finished contribution, and must not create pressure to accept a design because implementation work already exists. The code is disposable evidence that can be closed, replaced, or substantially rewritten after design discussion.

This work originally began as a possible Corvus.Json capability that would sit outside ASP.NET Core and replace or wrap the existing OpenAPI schema-generation path for applications that wanted stronger inference. Before establishing a separate integration surface and ecosystem convention, we concluded it would be better to present the capability as an in-box design proposal. The prototype exists to show what that option could look like in the real ASP.NET Core pipeline and to let maintainers decide whether any of it belongs in the framework. If the answer is no, the external-package direction remains available; the existence of working code is not an argument that ASP.NET Core must adopt it.

## In scope

- An experimental `Legacy`/`Inferred` schema-generation mode, with Legacy remaining the default.
- Immutable, cycle-safe facts derived from effective `JsonTypeInfo`, converters, serializer options, endpoint metadata, and recognized framework provenance.
- An optional public schema-evidence provider seam for strict runtime-enforced scalar and fixed positional-array facts that ordinary metadata cannot prove.
- Typed decisions for:
  - lossless inheritance composition;
  - `oneOf` versus `anyOf`;
  - discriminators;
  - exact/open finite enum domains;
  - closed, open, and extension-data object contracts;
  - directional input/output participation, nullability, and requiredness;
  - converter-proven scalar constraints;
  - binder-aware route/query/header/form parameter schemas.
- Deterministic document-wide component naming, collision handling, aliases, and late-graph planning.
- Authoritative custom reference IDs with explicit failure when one ID cannot represent divergent contracts.
- Positional JSON-array contracts for framework tuples when a recognized converter proves that representation.
- OpenAPI-version-aware emission and transformer contexts.
- Exact integral bounds through `Int128`/`UInt128` where the effective converter or binder proves the numeric domain.
- Version-aware base64 schemas:
  - OpenAPI 3.0: `format: byte`;
  - OpenAPI 3.1/3.2: `contentEncoding: base64`.
- Logical post-binding schemas for non-body parameters.
- A configurable scalar-format policy:
  - conventional well-known formats by default;
  - a compatible-only subset;
  - complete suppression of optional formats;
  - a type-, location-, purpose-, version-, and provenance-aware override callback.
- Reflection/source-generated and runtime/RDG parity where the platform exposes equivalent contracts.
- Trimming and NativeAOT-safe closed converter paths.

## Out of scope

- Inferring arbitrary business rules, control-flow conditions, or correlated-property dependencies.
- Reverse-engineering arbitrary System.Text.Json converters.
- Reverse-engineering arbitrary `TryParse`/`IParsable` lexical languages.
- Treating `BindAsync` as a scalar text parser.
- Inferring dictionary key domains without effective public property-name converter metadata.
- Inferring `uniqueItems` from set-like CLR types.
- Claiming that conventional formats exactly describe every runtime-accepted lexical form.
- Inventing floating-point precision, decimal scale, or `multipleOf` constraints.
- Changing Legacy output.
- Making inferred mode the default before compatibility, performance, and ecosystem review.
- Replacing application-owned schema transformers for domain-specific constraints.
- Treating the illustrative implementation or draft pull request as merge-ready work.
- Asking maintainers to perform line-by-line implementation review before the design direction is accepted.

## Risks / unknowns

- **Document churn:** stronger composition and deterministic directional identities intentionally change inferred documents. Opt-in mode and stable planning limit accidental churn.
- **Client-generator variability:** some generators have incomplete support for `allOf`, `oneOf`, JSON Schema 2020-12 keywords, or arbitrary formats. Version-aware fallbacks and compatibility testing are required.
- **Overconstraint:** an inferred keyword may reject data accepted by the runtime. The design treats unknown as a first-class result and requires recognized provenance before strengthening.
- **Underconstraint:** conservative broad schemas omit invariants known only to application code. Transformers remain the explicit escape hatch.
- **Performance and memory:** document-wide graph discovery and identity planning add work. Representative benchmarks and generation-memory measurements are needed before stabilization.
- **Public API maturity:** version-targeted generation and transformer context additions need normal API review. Internal fact and decision models should remain implementation details.
- **OpenAPI versus wire syntax:** non-body schemas describe logical post-binding values rather than every accepted HTTP lexical form. This preserves useful generated numeric types but must be documented clearly.
- **Ecosystem interpretation of `format`:** conventional mode intentionally emits familiar client-generation annotations even when the runtime accepts a broader lexical language. `CompatibleOnly`, `None`, and the callback allow applications to choose a stricter policy.
- **NativeAOT discovery limits:** RDG can register only contracts visible to its compile-time endpoint model. Explicit closed registration remains necessary for dynamic or opaque graphs.
- **Premature-implementation pressure:** a large implementation can look like a fait accompli or imply sunk-cost pressure. The companion pull request must remain draft, prominently state that it is not intended for merge, and serve only to make design tradeoffs, generated output, compatibility behavior, and feasibility concrete.

## Examples

### Opt-in usage

**Before — Legacy remains the default**

```csharp
builder.Services.AddOpenApi();
```

**After — Inferred is explicitly selected**

```csharp
builder.Services.AddOpenApi(options =>
{
    options.SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred;
});
```

The following examples are extracted from complete documents emitted by the same real Minimal API project before and after enabling inferred mode.

### Concrete emitted evidence

A standalone Minimal API project uses only public ASP.NET Core and Microsoft.OpenApi APIs:

- `AddOpenApi`;
- keyed `IOpenApiDocumentProvider`;
- `GetOpenApiDocumentForVersionAsync`;
- `OpenApiJsonWriter`;
- public AOT-safe tuple converter registration.

From the same application source and serializer configuration, it generates seven complete documents at commit `b3e5093c70d7751b2f7dceed1f0562a11b81b57f`:

| Mode | OpenAPI | Exact document |
| --- | --- | --- |
| Legacy | 3.0 | `legacy-oas30.json` |
| Legacy | 3.1 | `legacy-oas31.json` |
| Inferred | 3.0 | `inferred-oas30.json` |
| Inferred | 3.1 | `inferred-oas31.json` |
| Inferred + CompatibleOnly | 3.1 | `inferred-oas31-compatibleonly.json` |
| Inferred + None | 3.1 | `inferred-oas31-none.json` |
| Inferred + callback | 3.1 | `inferred-oas31-callback.json` |

No normalization is applied to the complete documents. Each is parsed again through `OpenApiDocument.Parse`. A checked-in extraction script creates deterministic proposal-sized comparisons by ordering object keys and removing unchanged operation boilerplate; it preserves arrays, references, keywords, numeric literals, required sets, formats, and discriminator mappings.

The complete reproducible evidence bundle contains:

- the Minimal API source and project;
- generation commands;
- all seven exact emitted documents;
- five focused before/after excerpts;
- cross-platform PowerShell extraction and validation scripts;
- a local .NET tool manifest pinning `Corvus.Json.Cli` 5.6.1.

Each generation run parses the OpenAPI container through `OpenApiDocument.Parse`. The validation script builds standalone JSON Schema 2020-12 roots that add only `$schema` and the target root `$ref`, preserving the exact emitted components and reference graph. `corvusjson validateDocument` accepts representative valid polymorphic, directional, and serializer payloads and rejects missing/unknown discriminators, missing required input, and out-of-range integral values.

The five comparisons cover:

1. directional request and response contracts;
2. lossless inheritance and structurally exclusive polymorphism;
3. effective serializer number handling and extension data;
4. scalar constraints, base64 version policy, and transport binding;
5. positional tuples.

#### Directional contract

**Before — Legacy**

Legacy points both request and response at one merged component:

```json
{
  "request": { "$ref": "#/components/schemas/DirectionalDto" },
  "response": { "$ref": "#/components/schemas/DirectionalDto" }
}
```

That component contains `inputOnly` and `outputOnly` together and marks the deserialization-only requirement `requiredButOmittable` as universally required.

**After — Inferred**

Inferred output points at distinct stable components:

```json
{
  "request": { "$ref": "#/components/schemas/DirectionalDto.Input" },
  "response": { "$ref": "#/components/schemas/DirectionalDto.Output" }
}
```

`DirectionalDto.Input` contains `inputOnly` and the input required set. `DirectionalDto.Output` contains `outputOnly` and does not claim that an input requirement is always emitted.

#### Inheritance and polymorphism

**Before — Legacy**

Legacy flattens `Customer`:

```json
{
  "Customer": {
    "type": "object",
    "properties": {
      "id": { "type": ["integer", "string"], "format": "int64" },
      "name": { "type": "string" }
    }
  }
}
```

Legacy polymorphism uses `anyOf`:

```json
{
  "Animal": {
    "type": "object",
    "required": ["kind"],
    "anyOf": [
      { "$ref": "#/components/schemas/AnimalCat" },
      { "$ref": "#/components/schemas/AnimalDog" }
    ],
    "discriminator": {
      "propertyName": "kind",
      "mapping": {
        "cat": "#/components/schemas/AnimalCat",
        "dog": "#/components/schemas/AnimalDog"
      }
    }
  }
}
```

**After — Inferred**

Inferred mode reuses the proven base contract:

```json
{
  "Customer": {
    "type": "object",
    "allOf": [
      { "$ref": "#/components/schemas/Entity" },
      {
        "type": "object",
        "properties": {
          "name": { "type": "string" }
        }
      }
    ]
  }
}
```

Explicit STJ polymorphism emits `oneOf` only when every referenced branch contains its matching discriminator literal:

```json
{
  "Animal": {
    "type": "object",
    "required": ["kind"],
    "oneOf": [
      { "$ref": "#/components/schemas/AnimalCat" },
      { "$ref": "#/components/schemas/AnimalDog" }
    ],
    "discriminator": {
      "propertyName": "kind",
      "mapping": {
        "cat": "#/components/schemas/AnimalCat",
        "dog": "#/components/schemas/AnimalDog"
      }
    }
  },
  "AnimalCat": {
    "type": "object",
    "properties": {
      "kind": { "type": "string", "enum": ["cat"] }
    }
  },
  "AnimalDog": {
    "type": "object",
    "properties": {
      "kind": { "type": "string", "enum": ["dog"] }
    }
  }
}
```

The validator script proves representative cat and dog payloads match exactly one branch, while missing or unknown discriminators fail. If branch-local exclusivity is not observable, generation retains `anyOf`.

#### Effective serializer contract

**Before — Legacy**

Legacy retains number/string acceptance but omits exact integral bounds, labels decimal as `double`, emits base64 as `format: byte` in OpenAPI 3.1, and loses the typed extension-data fallback:

```json
{
  "SerializerEnvelope": {
    "type": "object",
    "properties": {
      "amount": {
        "type": ["number", "string"],
        "format": "double",
        "pattern": "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?$"
      },
      "data": {
        "type": "string",
        "format": "byte"
      },
      "identifier": {
        "type": "string",
        "format": "uuid"
      },
      "relativeUri": {
        "type": ["null", "string"],
        "format": "uri"
      },
      "small": {
        "type": ["integer", "string"],
        "pattern": "^-?(?:0|[1-9]\\d*)$"
      }
    }
  }
}
```

**After — Inferred**

Inferred mode preserves the effective number/string contract, constrains only the numeric branch, retains base64 encoding vocabulary, and exposes the extension-data value schema:

```json
{
  "JsonElement": {},
  "SerializerEnvelope": {
    "type": "object",
    "additionalProperties": {
      "$ref": "#/components/schemas/JsonElement"
    },
    "properties": {
      "amount": {
        "type": ["number", "string"],
        "pattern": "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?$"
      },
      "data": {
        "type": "string",
        "contentEncoding": "base64"
      },
      "identifier": {
        "type": "string",
        "format": "uuid"
      },
      "relativeUri": {
        "type": ["null", "string"],
        "format": "uri-reference"
      },
      "small": {
        "type": ["integer", "string"],
        "minimum": -128,
        "maximum": 127,
        "pattern": "^-?(?:0|[1-9]\\d*)$"
      }
    }
  }
}
```

The referenced `JsonElement` component is deliberately the empty schema `{}`: each extension-data value may be any valid JSON value. Keeping the reference in the example is important because inferred mode preserves both the named object properties and the serializer-proven arbitrary-JSON fallback, rather than silently dropping the extension-data contract.

#### Scalars and transport binding

**Before — Legacy**

For OpenAPI 3.1, Legacy emits a relative-or-absolute `Uri` parameter with narrowing `format: uri`, models `BigInteger` as a JSON object component, labels decimal as `double`, and omits fixed-width bounds:

```json
[
  {
    "name": "routeValue",
    "in": "path",
    "required": true,
    "schema": {
      "type": ["integer", "string"],
      "format": "int32",
      "pattern": "^-?(?:0|[1-9]\\d*)$"
    }
  },
  {
    "name": "relativeUri",
    "in": "query",
    "required": true,
    "schema": {
      "type": "string",
      "format": "uri"
    }
  },
  {
    "name": "bigInteger",
    "in": "query",
    "required": true,
    "schema": {
      "$ref": "#/components/schemas/BigInteger"
    }
  },
  {
    "name": "amount",
    "in": "header",
    "required": true,
    "schema": {
      "type": ["number", "string"],
      "format": "double"
    }
  }
]
```

**After — Inferred**

Inferred mode emits the logical post-binding contracts:

```json
[
  {
    "name": "routeValue",
    "in": "path",
    "required": true,
    "schema": {
      "type": "integer",
      "format": "int32",
      "minimum": -2147483648,
      "maximum": 2147483647
    }
  },
  {
    "name": "relativeUri",
    "in": "query",
    "required": true,
    "schema": {
      "type": "string",
      "format": "uri-reference"
    }
  },
  {
    "name": "identifier",
    "in": "query",
    "required": true,
    "schema": {
      "type": "string",
      "format": "uuid"
    }
  },
  {
    "name": "bigInteger",
    "in": "query",
    "required": true,
    "schema": { "type": "integer" }
  },
  {
    "name": "amount",
    "in": "header",
    "required": true,
    "schema": { "type": "number" }
  }
]
```

The same exact documents demonstrate `contentEncoding: base64` in OpenAPI 3.1 and the compatible `format: byte` fallback in 3.0.

#### Scalar-format policy

**Before — Legacy**

Scalar formats are fixed by the existing generation path. Applications can change a finished schema with transformers, but they are not given the inferred candidate, provenance, binding source, purpose, or target version. For example, `System.Uri` is emitted as `uri` even when relative values are accepted.

**After — Inferred**

`Conventional` is the default and emits familiar client-generation annotations such as `uuid`, `uri-reference`, `date`, `date-time`, `time`, and numeric width formats.

Applications can globally select a stricter policy:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred;
    options.ScalarFormatPolicy = OpenApiScalarFormatPolicy.CompatibleOnly;
});
```

`CompatibleOnly` retains formats supported by the proven contract, such as built-in JSON `Guid` as `uuid`, built-in JSON `DateOnly` as `date`, and fixed integral widths. It omits conventional transport hints whose parsers accept broader forms.

`None` suppresses optional inferred formats while preserving schema types, numeric bounds, and proven base64 representation:

```csharp
options.ScalarFormatPolicy = OpenApiScalarFormatPolicy.None;
```

A callback can accept, replace, or suppress the policy-selected candidate for a specific type and binding context:

```csharp
options.CreateScalarFormat = context =>
{
    if (context.EffectiveType == typeof(Guid))
    {
        return "guid-custom";
    }

    if (context.EffectiveType == typeof(Uri) &&
        context.Location == OpenApiScalarFormatLocation.Query)
    {
        return null;
    }

    return context.DefaultFormat;
};
```

The callback sees the declared and effective types, body/property or transport location, input/output/neutral purpose, OpenAPI version, coarse converter/parser provenance, and default candidate. It runs before schema transformers, which retain final authority. Arbitrary non-null format names are emitted verbatim; callback exceptions propagate.

Proven base64 representation is deliberately outside this optional policy. OpenAPI 3.1/3.2 always use `contentEncoding: base64`, while OpenAPI 3.0 always uses `format: byte`.

#### Positional tuple

The same public AOT-safe tuple converter is registered in both modes.

**Before — Legacy**

Legacy emits the positional tuple but omits the exact numeric bound for its first element:

```json
{
  "ValueTupleOflongAndboolean": {
    "type": "array",
    "prefixItems": [
      {
        "type": ["integer", "string"],
        "format": "int64",
        "pattern": "^-?(?:0|[1-9]\\d*)$"
      },
      {
        "type": "boolean"
      }
    ],
    "minItems": 2,
    "maxItems": 2,
    "items": false
  }
}
```

**After — Inferred**

Inferred mode retains the positional contract and applies converter-proven bounds to the numeric branch:

```json
{
  "ValueTupleOflongAndboolean": {
    "type": "array",
    "prefixItems": [
      {
        "type": ["integer", "string"],
        "format": "int64",
        "minimum": -9223372036854775808,
        "maximum": 9223372036854775807,
        "pattern": "^-?(?:0|[1-9]\\d*)$"
      },
      {
        "type": "boolean"
      }
    ],
    "minItems": 2,
    "maxItems": 2,
    "items": false
  }
}
```

## Proposed API and review plan

The prototype adds experimental public surface in three separable packages:

1. inferred schema generation and scalar-format policy;
2. version-targeted generation and transformer visibility;
3. positional tuple JSON conversion and generated registration behavior.

The [complete proposed API appendix](api-surface.md) contains the exact signatures and explicit registration examples. These APIs should receive separate API reviews so agreement on the overall architecture does not force one decision across unrelated public surfaces.

The existing `IOpenApiDocumentProvider` remains unchanged; version targeting uses a derived experimental interface. Tuple conversion requires explicit HTTP JSON converter registration. `AddOpenApiCore` and `IAdditionalOpenApiDocumentNameResolver` already existed in the merge-base unshipped API file and are not part of this proposal.

## Detailed design

See [Architecture and extensibility](architecture.md) for the complete component model, generation lifecycle, existing-versus-proposed comparison, extension mechanisms, identity planning, version policy, and NativeAOT/RDG architecture. The sections below summarize the design decisions most relevant to proposal review.

The sections below describe one executable exploration, not a proposed merge plan. Design agreement should be based on the problem, goals, constraints, and architectural boundaries above. If the proposal is accepted, implementation should be replanned with maintainers and may be split, rewritten, or restarted rather than merging the illustrative branch.

### Contract authorities

The design has two independent authorities:

1. **JSON bodies and properties:** effective System.Text.Json `JsonTypeInfo`, converters, serializer options, and recognized converter provenance.
2. **Route/query/header/form parameters:** endpoint binding metadata, binding source, effective type, and recognized framework parser provenance.

CLR type identity contributes graph identity and known framework classification but cannot override an effective converter or prove an arbitrary parser language.

### Fact and decision layers

Cycle-safe immutable facts are discovered before OpenAPI emission. JSON facts describe object/property participation, directional nullability, collections, dictionaries, alternatives, polymorphism, converters, tuples, scalar provenance, and recursive edges. Transport facts separately describe binding source, parser provenance, logical type, repeated-value elements, optionality, and defaults.

Typed decisions consume those facts:

- inheritance eligibility and rejection reason;
- `oneOf`/`anyOf` exclusivity;
- exact/open literal domains;
- object fallback policy;
- input/output/neutral schema purpose;
- scalar bounds and encoding;
- logical transport schema.

Unknown is a normal conservative decision rather than a guessed success case.

### Identity and direction

The full reachable graph is planned before emission. Unique existing IDs are preserved; collisions are resolved symmetrically using declaring type, namespace, and canonical identity hash fallbacks. Late transformer-requested graphs use the same occupancy registry.

Request input, response output, and transformer-neutral purposes participate in graph identity. Directionally different default identities receive `.Input` and `.Output`; identical graphs share the existing identity. Configured custom IDs are authoritative and fail when they cannot distinguish divergent contracts.

Transport schemas remain inline or in a separate identity space and cannot alias JSON body components.

### Composition and domains

Inheritance uses `allOf` only when base reuse plus local derived constraints is lossless. Alternatives use `oneOf` only when JSON domains or exact finite literal sets prove pairwise exclusivity; otherwise they use `anyOf`.

Effective number handling participates in domain analysis. Numeric strings can overlap string alternatives, nullable branches share null, integer overlaps number, and arbitrary converter output remains unknown.

### Scalars and versions

Recognized built-in integral converters and binders produce exact bounds. Numeric keywords apply only to numeric branches. Floating-point precision and decimal scale are not inferred.

Recognized byte-array and byte-memory converters prove base64 representation. OpenAPI 3.0 uses `format: byte`; OpenAPI 3.1/3.2 use `contentEncoding: base64`.

Inferred mode also applies an experimental scalar-format policy before schema transformers:

- `Conventional` is the default and emits registered formats plus established client-generation width hints;
- `CompatibleOnly` emits the subset supported by the proven converter or parser contract;
- `None` suppresses optional formats;
- `CreateScalarFormat` can accept, replace, or suppress the candidate for each immutable scalar context.

The callback context distinguishes JSON body/property from route/query/header/form, input/output/neutral purpose, OpenAPI version, and built-in/custom/unknown provenance without exposing internal fact records. Custom converters and parsers have no default candidate, but applications may explicitly supply one. Proven base64 encoding is invariant under every policy and callback.

Tuple arrays use fixed arity in all versions, with `prefixItems` and `items: false` only where supported.

The OpenAPI target is selected before schema generation and transformers. A version-sensitive document must be regenerated, not merely reserialized as another version.

### Extensibility

Schema, operation, and document transformers continue to own application-specific constraints. Their contexts receive the authoritative generation target, and traversal covers composed branches, properties, items, dictionary fallbacks, extension data, tuple elements, and finalized transport schemas.

Recognized package-owned converter provenance can supply immutable declarative facts for otherwise opaque behavior. Arbitrary converter implementation details are not inspected.

### Compatibility and validation

Legacy remains the default and is covered by version-neutral compatibility tests. The implementation branch includes focused fact/decision tests, endpoint document tests, runtime/RDG parity, reflection/source-generated parity, all three OpenAPI versions, analyzer/PublicAPI checks, build-time generation, trimming, NativeAOT, and benchmark validation.

The latest complete validation on the branch reported:

- full managed OpenAPI suite: 1,347 passed, 5 skipped, 0 failed;
- exact OpenAPI build: 0 warnings and 0 errors;
- source-generator, build-time, analyzer/PublicAPI, trimming, NativeAOT, and benchmark validation passed.

These results demonstrate feasibility and expose behavioral consequences for discussion. They are not presented as evidence that the current branch should be merged.

### Drawbacks

- The internal architecture is more sophisticated than direct schema translation.
- Inferred documents intentionally differ from Legacy documents and require migration review.
- Conservative unknowns can still be less precise than application authors desire.
- Some client generators may not consume advanced composition consistently.
- Logical transport schemas do not encode every accepted wire spelling.

### Considered alternatives

#### Extend direct schema translation with more CLR-type tables

Rejected because CLR identity does not prove effective converter or parser behavior, and ad hoc tables mix facts, policy, and emission.

#### Make stronger generation the default

Rejected for the experimental phase because existing applications may compare documents or depend on established component layouts and client hints.

#### Use raw string schemas and patterns for all non-body parameters

Rejected because several framework parser languages cannot be represented faithfully or portably, numeric ranges become awkward in OpenAPI 3.0, and generated clients lose useful logical types.

#### Retain familiar formats as harmless annotations

Rejected because validators and generators may enforce `date-time`, `time`, and `uri`; these formats can exclude values accepted by ASP.NET Core binding.

#### Infer from constructors, control flow, collection interfaces, or converter generic arguments

Rejected because these are not authoritative declarative runtime contracts.

### Open questions

- Which inferred behaviors should eventually stabilize, and should they remain behind a mode after stabilization?
- What representative applications and client generators should be used for compatibility evaluation?
- Are additional public transformer APIs needed for schema purpose, or is neutral late-schema generation sufficient?
- Should a future public declarative fact-provider contract exist for package-owned converters or parsers?
- Can System.Text.Json expose effective dictionary property-name metadata in a form suitable for safe inference?
- What performance and allocation budget should document-wide planning meet?

### References

- [Benefits and proposal framing](benefits.md)
- [Architecture and extensibility](architecture.md)
- [Proposed API surface and review sequencing](api-surface.md)
- Implementation branch: <https://github.com/endjin/aspnetcore/tree/openapi-schema-inference>
- Companion draft PR: to be added after the design-proposal issue is filed. It will be marked **illustrative only — not intended for merge**.
- Reproducible evidence bundle: attach the Minimal API project, seven exact emitted documents, focused excerpts, PowerShell scripts, and pinned Corvus validation tooling when filing.
- ASP.NET Core contribution guidance: <https://github.com/dotnet/aspnetcore/blob/main/CONTRIBUTING.md>
- ASP.NET Core API review process: <https://github.com/dotnet/aspnetcore/blob/main/docs/APIReviewProcess.md>
- System.Text.Json JSON Schema exporter documentation: <https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/extract-schema>
- OpenAPI Specification: <https://spec.openapis.org/oas/latest.html>
