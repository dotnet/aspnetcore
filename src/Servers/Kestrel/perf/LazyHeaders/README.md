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

The generalized Core selection passes **9317 cases**. It includes fresh independent read surfaces for eligible known names plus X-Example and typed-unknown Alt-Svc, on/off; source overwrite; duplicates; no-decode mutation; presence/empty/missing semantics; arena growth/exact budget/fallback/clearing; reset/reuse; custom encoding and malformed unread values; CopyTo argument errors without materialization; and existing header/parser/HTTP/1 tests.

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

The first three generalized pilot jobs at `6f75f595` had positive load/server measurements and zero errors; 96 socket requests passed exact body/length/checksum checks. The first full-duration baseline job subsequently reported **seven socket timeouts**, zero bad responses and 1,192,592 requests. Its error guard stopped the plan. It is retained, not a clean comparative result, and not evidence of a feature regression (the feature was absent).

Before continuing, an additional CopyTo argument-compatibility correction was made and tested. The corrected source/payload will receive a new frozen plan. To avoid repeatedly abandoning or selecting results based on timeout noise, that plan records socket errors for **every predetermined case**, without filtering/replacement, while bad responses or missing/invalid measurements still fail. This changes the runner's stopping policy, not client load settings, and any resulting comparison must disclose errors. Initial pilots/failed plan remain separate from corrected-head evidence.

## Local Crank reproduction

Use existing local resources, original POC runtime `12.0.0-alpha.1.26473.114` (do not retarget the application), SDK `11.0.100-rc.1.26420.103`, and server GC as recorded in the runtimeconfig. Controller/agent/wrapper source is pinned to Crank `0d35cad5102892b091d9a88a52ea1f7a91bb1381`, built/published in Release on SDK 10.0.402/runtime 10.0.13. Build the official wrk 4.2.0 source at `a211dd5a7050b1f9e8a9870b95513060e72ac4a0` with `make -j2`; the frozen local binary is `d689dd6384794d71132d6f33b7377a6da6e9e73455d1c66d02d1583d0b73d446`.

In a net10 tool shell (not the activated POC SDK), publish the pinned `src/Microsoft.Crank.Jobs.Wrk/Microsoft.Crank.Jobs.Wrk.csproj` to `$WRAPPER`; build pinned controller/agent projects in Release. `$CRANK_DLL` and `$AGENT_DLL` are their `artifacts/bin/Microsoft.Crank.{Controller,Agent}/Release/net10.0/{crank,crank-agent}.dll` outputs. Set absolute `$REPO`, `$HEAD_PUBLISH`, `$BASE_PUBLISH`, `$POC_DOTNET_ROOT`, `$WRAPPER`, `$NET10_ROOT` and an immutable 40-character `$SOURCE` containing the workload. Publish the application with the POC SDK into `$HEAD_PUBLISH`; copy that entire folder to `$BASE_PUBLISH` and replace **only** its Core DLL with the baseline `2d105dd1fd119edff9a797a1b1aeca6d771c6134` build, SHA-256 `7175498ad5d77f11fa7672fe89bf4a51ad7a8b8410ed1cbc4c85906ee1fe27f5`.

```sh
export TMPDIR=/tmp/lhp-7a143fff
mkdir -p "$TMPDIR/.crank"
cp "$WRK_BINARY" "$TMPDIR/.crank/wrk-linux-amd64"
# In a separate foreground shell; stop only this owned agent after its queue is empty.
dotnet "$AGENT_DLL" --url http://127.0.0.1:15010 --hostname 127.0.0.1 \
  --dotnethome "$NET10_ROOT" --build-path "$TMPDIR/jobs"
```

Verify `curl --fail http://127.0.0.1:15010/jobs` is empty before running:

```sh
python "$REPO/src/Servers/Kestrel/perf/LazyHeaders/generalized/smoke.py" \
  --head "$HEAD_PUBLISH" --base "$BASE_PUBLISH" --runtime "$POC_DOTNET_ROOT" \
  --output "$SMOKE_OUTPUT"
python "$REPO/src/Servers/Kestrel/perf/LazyHeaders/generalized/run.py" \
  --controller "$CRANK_DLL" --repo "$REPO" --head "$HEAD_PUBLISH" \
  --base "$BASE_PUBLISH" --runtime "$POC_DOTNET_ROOT" --wrapper "$WRAPPER" \
  --source "$SOURCE" --output "$PRIMARY_OUTPUT" --record-errors
```

The runner fixes 16 connections, two threads, 10-second timeout, 15-second warmup, 30-second measurement, pipeline one and five interleaved repetitions. It renders/asserts legacy `--application.source.localFolder` and `--load.source.localFolder` payloads, clears both `--load.source.project ""` and `--load.project ""`, and supplies **both** `pocDotnetRoot` and `wrkWrapperPublish`. Using plural `--application.sources.default.localFolder` can silently select the wrong source. Same short TMPDIR for agent/controller/application/load is required for EventPipe socket discovery.

Every run retains resolved config, exact command/timestamps, controller output, raw result/counter arrays, and actual uploaded Core/runtime identities. Inputs record full per-file payload, wrapper and shared-runtime manifests. `--resume` verifies source/settings and preserves interrupted directories; it never reruns/replaces completed cases. `--record-errors` retains socket errors, not bad responses or missing metrics. Missing metrics are not zero. No best-run selection or allocation-rate/total-request shortcut is used.

## Historical evidence

All prior microbenchmarks, socket checks, pilots, setup errors, interrupted runs, 60 primary User-Agent-only Crank runs and nine single-repeat supplemental cases remain unchanged under their original paths and commits. See [historical methodology](historical-user-agent.md), [raw archives and checksums](crank-local) and [original verification log](verification.txt). In particular the supplemental baseline enumeration's 15 timeouts and earlier default-off 16 timeouts are retained, not replaced or pooled into error-free primary results.
