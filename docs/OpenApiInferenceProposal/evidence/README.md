# OpenAPI inference evidence

This standalone Minimal API project references the prototype branch's
`Microsoft.AspNetCore.OpenApi` project and uses public ASP.NET Core and
Microsoft.OpenApi APIs to generate exact documents.

Branch commit:

```text
7e7b647ef269dbdbf118c6052c2657f98b36626e
```

True merge base with `main`:

```text
89ab93803f3fcbb928f8ed1523945f89284f7e79
```

## Reproduction

The recorded run used PowerShell 7.6.2 on Ubuntu 22.04.5 LTS under WSL, the
repository's .NET SDK `11.0.100-rc.1.26420.103`, and ASP.NET Core runtime
`11.0.0-rc.1.26420.103`. Native Windows and macOS runs were not performed.
See `validation/environment.txt` for the exact environment, fork checkout/build
prerequisites, enabled public package-feed families, and disclosure boundaries.

Activate the `dotnet/aspnetcore` repository environment first (`activate.ps1`
on Windows or `activate.sh` on Linux/macOS). Build the repository OpenAPI/HTTP
prerequisites with the repository build before this standalone project. Then
run the following with PowerShell 7:

```powershell
$env:AspNetCoreRepoRoot = (Resolve-Path '<path-to-aspnetcore>').Path
Set-Location '<path-to-openapi-evidence>'
dotnet tool restore
dotnet build --no-restore

dotnet run --no-build -- Legacy 3.0
dotnet run --no-build -- Legacy 3.1
dotnet run --no-build -- Legacy 3.2
dotnet run --no-build -- Inferred 3.0
dotnet run --no-build -- Inferred 3.1
dotnet run --no-build -- Inferred 3.2
dotnet run --no-build -- Inferred 3.1 CompatibleOnly
dotnet run --no-build -- Inferred 3.1 None
dotnet run --no-build -- Inferred 3.1 Callback

pwsh -NoProfile -File ./extract.ps1
pwsh -NoProfile -File ./validate.ps1
```

`OpenApiEvidence.csproj` suppresses `ASP0040` because the evidence intentionally
exercises the branch's experimental public APIs. That suppression is local to
this project. `AspNetCoreRepoRoot` must identify the accepted fork checkout at
the exact commit above; this project is not meaningful against an arbitrary
installed shared framework.

`Corvus.Json.Cli` 5.6.1 is a third-party validation tool pinned in
`.config/dotnet-tools.json` and restored locally by `dotnet tool restore`; no
global tool install is used. The restore uses the checkout's configured public
dnceng feeds. `validation/corvus-transcript.txt` records the sanitized commands,
expectations, rationales, outcomes, and tool output so review does not require
executing the tool.

## Artifacts and normalization

Each `dotnet run` writes one complete JSON document under `documents/` and
parses it again with `OpenApiDocument.Parse`. The nine files are exact,
unmodified public-provider/OpenAPI-writer output. No normalization is applied
to complete documents.

`extract.ps1` creates five proposal-sized comparison files under `excerpts/`.
It requires every selected key by exact ordinal spelling, selects the same
property and parameter names for both sides of each scalar/transport
comparison, copies the selected JSON values without semantic rewriting, and
recursively orders JSON object keys ordinally for deterministic presentation.
It intentionally omits unselected document boilerplate. These are normalized
extracts, not complete documents or hand-authored summaries.

`validate.ps1`:

- requires case-sensitive keys and emits purpose-written missing/extra-key
  errors rather than relying on StrictMode property failures;
- parses all nine complete documents;
- checks representative Legacy/Inferred and OpenAPI 3.0/3.1/3.2 invariants;
- checks annotation presence (`format`, `contentEncoding`, and OpenAPI
  discriminator) with PowerShell;
- asserts symmetric scalar/transport excerpt selectors; and
- creates JSON Schema 2020-12 wrappers from the exact emitted OpenAPI 3.1
  component graph, adding only `$schema` and a root `$ref`.

Corvus validates structural JSON Schema assertions in those wrappers. It does
not validate the OpenAPI container and does not assign assertion semantics to
OpenAPI discriminator, `format`, or `contentEncoding` annotations. The public
generator performs the separate OpenAPI parse.

## Focused comparisons and practical impact

1. **Directional DTO** — Inferred separates `DirectionalDto.Input` and
   `DirectionalDto.Output`, retaining setter/getter nullability and requiredness.
   Validators and generated clients can represent request and response contracts
   independently.
2. **Inheritance and polymorphism** — Inferred uses lossless `allOf`
   inheritance and stable references. Explicit STJ polymorphism uses `oneOf`
   only when each branch has a structural discriminator literal; otherwise
   generation conservatively uses `anyOf`. Corvus accepts representative cat
   and dog payloads and rejects missing/unknown discriminator payloads by JSON
   Schema assertions. PowerShell separately checks the OpenAPI discriminator
   annotation.
3. **Effective serializer contract** — Inferred records numeric-or-quoted
   numeric input from `JsonNumberHandling` and preserves mixed named properties
   plus typed extension data. This improves validator alignment with effective
   serializer input without claiming an exact language for every parser.
4. **Scalars and transport** — The symmetric extract contains `small`,
   `amount`, `data`, `identifier`, `relativeUri`, and `name` on both sides, plus
   the same nine route/query/header parameters. It shows:
   - exact integral numeric-instance bounds;
   - removal of Legacy's misleading decimal `double` annotation;
   - OpenAPI 3.0 `byte` versus OpenAPI 3.1/3.2 base64
     `contentEncoding`;
   - conventional `uuid`, `uri-reference`, and `date-time` annotations;
   - `IPAddress`/`IPEndPoint` Legacy object references versus inferred
     transport strings without narrowing formats; and
   - the 3.1 `CompatibleOnly`, `None`, and callback
     replacement/suppression results, including contextual `.Input`/`.Output`
     identities.
5. **Positional tuple** — Inferred OpenAPI 3.1 emits an array contract with
   `prefixItems`. The app explicitly registers the public AOT-safe
   `JsonArrayTupleConverters` converter. Package presence, RDG, `AddOpenApi`,
   and Inferred mode remain inert without that application opt-in; repository
   runtime tests cover the default, RDG on/off, dynamic and closed registration,
   frozen options, and user precedence.

The exact Legacy/default-Inferred OpenAPI 3.2 documents substantiate only
default 3.2 generation and the selected contract/annotation checks. Policy and
callback variants in this package are OpenAPI 3.1 evidence; no broader 3.2
policy matrix is claimed.

## Deliberate conservative boundaries

- The numeric branch for `sbyte` has exact `minimum: -128` and `maximum: 127`.
  Corvus therefore rejects numeric `128`.
- The quoted-number branch models integer lexical shape, not every CLR
  integral range. Corvus accepts string `"128"`, while the public app's
  System.Text.Json probe using the same serializer options rejects it for
  `sbyte`. `validation/runtime-probe.json` records that runtime result. This is
  a known conservative underconstraint; the package does not claim all quoted
  out-of-range integral values are rejected.
- JSON Schema numbers are mathematically arbitrary precision. Many OpenAPI and
  JavaScript-oriented tools instead use IEEE-754 number representations, so
  large `Int64`, `UInt64`, `Int128`, and `UInt128` bounds may not remain exactly
  representable in every downstream tool.
- `uint32` and `uint64` are established custom client-generation hints used by
  this feature, not universally registered OpenAPI Initiative formats.
  Likewise, `float` and `double` are client hints rather than exact accepted
  numeric domains.
- No `duration` format is inferred for `TimeSpan`; opaque custom converters and
  parsers have no candidate unless the callback supplies one; transport schemas
  do not alias JSON-body components; and no decimal `multipleOf` or floating
  precision bound is invented.
- `legacy-stability.md` records the exact merge base and the strongest
  reproducible compatibility evidence. The branch-added APIs prevent compiling
  this same public app at the merge base, so no byte-for-byte merge-base public
  app claim is made.
- Repository benchmark projects were built/validated and their existing runs
  completed as part of the exact OpenAPI build. No comparative schema-generation
  timing or allocation measurements have been collected.

The Corvus results are representative structural semantic spot-checks, not a
proof of all endpoints, payloads, validators, generators, or tooling.
