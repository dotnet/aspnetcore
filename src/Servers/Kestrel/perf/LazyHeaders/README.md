# Lazy HTTP/1.1 request-header values

Discussion POC for [#69962](https://github.com/dotnet/aspnetcore/issues/69962) and [draft #69963](https://github.com/dotnet/aspnetcore/pull/69963), not a shipping recommendation. **Current implementation covers ordinary known and unknown header values, not just User-Agent. Generalized end-to-end results are pending.** The prior 60-run plus nine supplemental User-Agent-only comparisons remain [historical evidence](historical-user-agent.md), not validation or performance evidence for this implementation.

## Hypothesis and alternatives

Changing values that applications do not read need not become strings. Existing Kestrel already interns known names and reuses stable decoded known values: this experiment must beat that baseline, not a hypothetical always-allocating implementation.

| Representation | Decision and tradeoff |
| --- | --- |
| Existing generated known slots, presence bits and string reuse | Preserve lookup/presence and reuse decoded stable values before copying. Framing controls stay eager. |
| General lazy values in owned bytes | Implement a single reusable, geometrically grown arena, bounded to 4096 copied bytes per request and retained bytes per collection. Known slots and one authoritative unknown dictionary hold decoded-or-range values. |
| Borrowed parser/input buffers | Do not retain them: callbacks can receive stack/pooled spans and input is advanced before application reads. |
| Response raw serialization buffers | Useful prior representation, but not a request input-ownership solution. No response/trailer store changes. |
| Lazy names | Defer. Names remain eagerly validated, known names predefined, and unknown names interned where existing code does so. Lookup and duplicate merging remain ordinal-ignore-case. |
| Ignored-header/materialization hints | Defer; no existing consumer contract was found. No headers are dropped or made unobservable. |

Public prior art: [#38585](https://github.com/dotnet/aspnetcore/pull/38585) and [#42681](https://github.com/dotnet/aspnetcore/pull/42681) concern reuse correctness; [#31492](https://github.com/dotnet/aspnetcore/issues/31492) concerns known-header memory/coverage; [#27467](https://github.com/dotnet/aspnetcore/issues/27467) concerns typed APIs. These are not verified lazy-allocation performance claims.

## Boundaries and correctness

Set `Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyRequestHeaders` before constructing Kestrel. It is **default-off and unsupported**. The old `LazyUserAgent` switch is not broadened or aliased.

Only the explicit HTTP/1.1 parser callback opts in. HTTP/1.0, shared literal ingestion, HPACK/QPACK indexed ingestion, HTTP/2/HTTP/3 and trailers remain eager. Shared collection layout/accessor costs still affect these paths; unchanged eager behavior does not imply zero performance impact.

All ordinary known and unknown values are eligible except **Host, Content-Length, Connection and Transfer-Encoding**, which remain eager for host validation, framing and connection control. Cookie, Authorization, Expect, Upgrade and Content-Type are not blanket exceptions. Framework reads can materialize them normally.

Default-encoded ASCII values are validated at ingestion, including NUL and conditional CR/LF rejection. Custom encoding selectors, non-ASCII, values over 4096 bytes, exhausted aggregate copy budget and duplicates use existing eager decoding. Invalid UTF-8 still fails before application access. Empty values remain present. The selector still receives the original unknown-name casing at ingestion.

The byte arena owns copies, never input slices. Offsets survive growth. Growth clears the old used buffer; materialization, overwrite/removal and reset clear sensitive raw ranges. Discarded holes count toward the request's 4096-byte budget; they are not reclaimed mid-request. GC ownership avoids pooled-buffer disposal/abort obligations. Arena size/budget are experimental choices, not tuned results.

Generated known properties and all unknown access paths materialize once on value reads. Set/overwrite/remove discard unread bytes without decoding; Add-existing checks presence. A second duplicate materializes the first value, appends in wire order, and all subsequent duplicates remain eager. Count, HasX, ContainsKey and Keys do not materialize. CopyTo, both indexers, typed APIs, TryGetValue, Values, pair operations and struct/generic/nongeneric/cached enumeration observe complete values. Unknown enumeration resolves the authoritative entry without structurally mutating the dictionary. Reset excludes unread slots from cross-request reuse, while decoded stable known strings and pseudo-header reuse retain existing behavior.

Tradeoffs include scanning/copying/secret clearing, per-known-slot metadata, larger unknown entries, first-read cost, retained capacity and cold-connection memory. Default-off is a measured control, not assumed free. No shipping/regression budget is established.

## Validation and workload

The generalized Core selection passes **9316 cases** at the current working source. It includes fresh independent read surfaces for eligible known names plus X-Example and typed-unknown Alt-Svc, on/off; source overwrite; duplicates; no-decode mutation; presence/empty/missing semantics; arena growth/exact budget/fallback/clearing; reset/reuse; custom encoding and malformed unread values; and existing header/parser/HTTP/1 tests.

Hosted coverage exercises four lazy known/unknown values, fragmented 255/256/257-byte values, body consumption before access, changed/absent/duplicate keep-alive values, eager HTTP/1.0, HTTP/2 and QPACK compatibility, and switch-enabled Host/Content-Length errors. The final broader HTTP/2/HTTP/3 selection passes **777 cases, five platform/TLS skips**. Two earlier selections each failed a different existing asynchronous test (timeout notification and incomplete-header injection); those nine focused cases subsequently passed together, followed by the passing full selection. Failed logs are retained; the passing repeat does not erase intermittency or establish a cause. Two private-layout reflection probes were adjusted from fields to properties while preserving their pooled-value clearing assertions.

After the existing repository bootstrap and `source activate.sh`:

```sh
dotnet test src/Servers/Kestrel/Core/test/Microsoft.AspNetCore.Server.Kestrel.Core.Tests.csproj \
  --no-restore -c Release \
  --filter 'FullyQualifiedName~HttpRequestHeaders|FullyQualifiedName~HttpHeadersTests|FullyQualifiedName~HttpParserTests|FullyQualifiedName~Http1ConnectionTests'
dotnet test src/Servers/Kestrel/test/InMemory.FunctionalTests/InMemory.FunctionalTests.csproj \
  --no-restore -c Release \
  --filter 'FullyQualifiedName~Http2|FullyQualifiedName~Http3|FullyQualifiedName~LazyRequestHeadersTests'
```

Reproduce generated output from `src/Servers/Kestrel` with the repository SDK:

```sh
dotnet run --no-restore -c Release --project tools/CodeGenerator/CodeGenerator.csproj -- \
  Core/src/Internal/Http/HttpHeaders.Generated.cs Core/src/Internal/Http/HttpProtocol.Generated.cs \
  Core/src/Internal/Infrastructure/HttpUtilities.Generated.cs \
  shared/TransportMultiplexedConnection.Generated.cs shared/TransportConnection.Generated.cs
```

The workload uses fixed-length stable/changing Accept, Accept-Language, User-Agent, X-Example and X-Request-Nonce, with Host/framing fixed. Access modes are ignore/read-one/read-all/enumerate. Supplemental modes duplicate a known or unknown header or exhaust the arena budget. The application exposes observed length and character-sum checksum; socket smoke checks must assert these plus exact response bytes. Wrk bad/socket counts alone do not assert response bodies.

The intended primary comparison remains five interleaved repetitions of stable/changing x ignore/read-all x baseline/head-off/head-on, with identical application/dependencies/runtime and only the Core DLL replaced for baseline. Local shared-client/server limits, all errors, full run order and timestamped allocation/GC/CPU/working-set samples must remain explicit. Allocation rate in B/s is not bytes/request; no maximum-rate/total-request division is valid.

## Historical evidence

All prior microbenchmarks, socket checks, pilots, setup errors, interrupted runs, 60 primary User-Agent-only Crank runs and nine single-repeat supplemental cases remain unchanged under their original paths and commits. See [historical methodology](historical-user-agent.md), [raw archives and checksums](crank-local) and [original verification log](verification.txt). In particular the supplemental baseline enumeration's 15 timeouts and earlier default-off 16 timeouts are retained, not replaced or pooled into error-free primary results.
