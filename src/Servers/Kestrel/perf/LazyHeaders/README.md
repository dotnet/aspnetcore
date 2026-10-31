# Lazy HTTP/1.1 request-header values

Discussion POC for [#69962](https://github.com/dotnet/aspnetcore/issues/69962) and [draft #69963](https://github.com/dotnet/aspnetcore/pull/69963), not a shipping recommendation. **Current implementation covers ordinary known and unknown header values, not just User-Agent. The generalized 60-run primary and 24-run supplemental Crank comparisons are complete and preserved: unread allocation rate falls, but errors, shared-host uncertainty and read/default-off costs prevent an overall throughput recommendation.** The prior User-Agent-only comparisons remain [historical evidence](historical-user-agent.md), not generalized results.

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

Typed-property reflection is case-insensitive so canonical lowercase gRPC/tracing names exercise their actual typed getters rather than falling back to indexers. The complete 9317-case selection passes again after this coverage correction; [log](generalized/final-core-reader-verification.tar.gz). Shipping code and frozen measured payloads are unchanged.

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

The workload uses fixed-length stable/changing Accept, Accept-Language, User-Agent, X-Example and X-Request-Nonce, with Host/framing fixed. Access modes are ignore/read-one/read-all/enumerate; **read-all reads those five fixture fields**, whereas enumerate reads every header. Supplemental modes duplicate a known or unknown header or add a 4000-byte X-Budget (see its reuse-dependent limitation below). The application exposes observed length and character-sum checksum; socket smoke checks must assert these plus exact response bytes. Wrk bad/socket counts alone do not assert response bodies.

The completed primary comparison uses five interleaved repetitions of stable/changing x ignore/read-all x baseline/head-off/head-on, with identical application/dependencies/runtime and only the Core DLL replaced for baseline. Local shared-client/server limits, all errors, full run order and timestamped allocation/GC/CPU/working-set samples are retained. Allocation rate in B/s is not bytes/request; no maximum-rate/total-request division is valid.

The first three generalized pilot jobs at `6f75f595` had positive load/server measurements and zero errors; 96 socket requests passed exact body/length/checksum checks. The first full-duration baseline job subsequently reported **seven socket timeouts**, zero bad responses and 1,192,592 requests. Its error guard stopped the plan. It is retained, not a clean comparative result, and not evidence of a feature regression (the feature was absent).

Before continuing, an additional CopyTo argument-compatibility correction was made and tested. The corrected source/payload received a new frozen plan. To avoid repeatedly abandoning or selecting results based on timeout noise, that plan records socket errors for **every predetermined case**, without filtering/replacement, while bad responses or missing/invalid measurements still fail. This changes the runner's stopping policy, not client load settings. Initial pilots/failed plan remain separate from corrected-head evidence.

## Completed generalized primary comparison

**60/60 predetermined runs completed, 67,899,721 measured requests, zero bad responses, 324 wrk-reported socket timeouts across 30 runs.** By variant, baseline/off/on reported **124/106/94** timeouts. These are not zero-error performance results and are not filtered, replaced, pooled with historical runs or attributed to a feature defect. All runs have positive RPS/latency metrics, server/load measurements and verified uploaded Core/runtime identities. The raw archive contains **188,023 timestamped measurements**.

[Full raw archive, commands and reproduction assets](generalized/primary-raw.tar.gz), [all per-run values and sample counts](generalized/primary-derived-runs.csv), [group and paired distributions](generalized/primary-derived-summary.json), [exact run order](generalized/primary-plan.json), [inputs/hashes](generalized/primary-inputs.json), [completion record](generalized/primary-completed.json) and [shared-host environment](generalized/environment.json).

Median RPS over five runs; parentheses show the complete observed range, not confidence intervals:

| Case | Baseline | Head off | Head on |
| --- | ---: | ---: | ---: |
| Ignore stable | 37794 (35378-39287) | 38625 (38382-39550) | 39049 (38027-39483) |
| Ignore changing | 37096 (36415-37822) | 37943 (35919-38409) | 38526 (35977-39109) |
| Read-all stable | 38825 (36820-38889) | 37580 (36015-38487) | 36960 (35152-38473) |
| Read-all changing | 37461 (35993-38400) | 36632 (36460-38054) | 37278 (35633-38483) |

Decimal allocation MB/s is the median of per-run interior-window means. P99 is the median reported latency, **excluding samples wrk classifies as timed out**:

| Case | Allocation MB/s base / off / on | P99 ms base / off / on | Timeouts base / off / on |
| --- | ---: | ---: | ---: |
| Ignore stable | 16.078 / 16.690 / 10.680 | 6.43 / 5.51 / 5.41 | 35 / 21 / 40 |
| Ignore changing | 24.753 / 26.085 / 10.568 | 6.84 / 5.22 / 5.48 | 36 / 42 / 8 |
| Read-all stable | 17.724 / 17.555 / 16.685 | 5.30 / 6.01 / 6.44 | 25 / 12 / 35 |
| Read-all changing | 26.569 / 26.061 / 26.047 | 5.26 / 6.41 / 6.07 | 28 / 31 / 11 |

Changing unread values show a substantial allocation-rate reduction. Stable known values already reuse decoded strings on baseline; stable unknown **values** still allocate on baseline, explaining the additional unread-stable saving. Names remain eager. Reading all changing values removes that allocation advantage.

**No trustworthy overall throughput improvement or acceptable regression budget is established.** Paired on/base RPS changes for changing/ignore have median +1.86%, range -1.81% to +7.03%. Read-all/stable has median -2.26%, range -9.61% to +0.38%; its default-off control has median -2.54%, range -7.24% to +2.06%. Read-all/changing on/base has median -2.59%, range -4.03% to +5.50%. Read and default-off costs remain concerns, not waived by unread allocation savings.

The pinned [wrk completion path](https://github.com/wg/wrk/blob/a211dd5a7050b1f9e8a9870b95513060e72ac4a0/src/wrk.c#L344-L345) increments its timeout counter when a latency cannot enter the bounded histogram; [stats_record](https://github.com/wg/wrk/blob/a211dd5a7050b1f9e8a9870b95513060e72ac4a0/src/stats.c#L22-L24) rejects out-of-range samples. Therefore reported P99/max do not bound timeout-classified observations. No root cause or specific transport failure is inferred from that counter alone. The official client was not patched and all errors remain visible.

Allocation/GC/CPU/working-set means use an approximate trimmed interior from 27 to three seconds before the first wrapper RPS event, avoiding startup/warmup/completion. Every run has **24 server allocation samples**. There is no aligned request-count denominator and no bytes/request claim. Missing metrics remain null/empty, not zero; singleton standard deviation is null. Coarse zero time-in-GC samples do not imply no collections. Changing/ignore median Gen0/s is 9.46/10.00/4.08 base/off/on.

Server CPU medians are about 111-113 core-percent, client 48-51, with roughly 73-75 MB server working set on this shared two-processor x64 host. No client/server bottleneck independence, isolated capacity or cold/low-reuse memory outcome is established. The retained arena/metadata costs and experimental 4096-byte budget remain discussion items.

## Completed generalized supplemental diagnostics

**24/24 predetermined runs completed, 26,054,458 measured requests, zero bad responses, nine wrk-reported timeouts across three runs.** Baseline/off/on timeouts are **0/2/7**: on/ignore/duplicate-unknown has two, off/read-all/budget two, and on/read-all/budget five. All other supplemental runs have zero reported errors. These remain separate from primary distributions: **one observation per variant/case, no repeat spread or significance claim**. The archive retains 75,380 timestamped measurements and 24 selected server allocation samples per run.

[Raw results, commands, identities and reproduction assets](generalized/supplemental-raw.tar.gz), [per-run metrics](generalized/supplemental-derived-runs.csv), [summary](generalized/supplemental-derived-summary.json), [fixed plan](generalized/supplemental-plan.json), [inputs/hashes](generalized/supplemental-inputs.json), [completion record](generalized/supplemental-completed.json). [SHA-256 manifest for all generalized artifacts](generalized/artifact-sha256.txt).

| Access / header mode | RPS base / off / on | Allocation MB/s base / off / on |
| --- | ---: | ---: |
| Read-one / changing | 36540 / 36107 / 38309 | 25.520 / 26.416 / 16.049 |
| Enumerate / changing | 37735 / 38294 / 36730 | 26.853 / 27.613 / 26.482 |
| Ignore / duplicate-known | 38633 / 37756 / 37280 | 23.547 / 22.653 / 17.089 |
| Read-all / duplicate-known | 38440 / 36014 / 38170 | 24.788 / 22.832 / 24.367 |
| Ignore / duplicate-unknown | 37016 / 37358 / 37409 | 20.594 / 20.803 / 18.650 |
| Read-all / duplicate-unknown | 37270 / 36433 / 36561 | 22.192 / 21.450 / 21.501 |
| Ignore / budget | 32642 / 31941 / 31029 | 278.097 / 269.738 / 257.475 |
| Read-all / budget | 32469 / 32605 / 34122 | 278.887 / 280.054 / 17.017 |

Duplicate and budget modes use **stable**, not changing, primary values. Duplicate ingestion forces the duplicated field eager; other unread fields can still defer. Reading one leaves other fields unread; enumerating materializes them. These diagnostic observations do not establish performance acceptance or negate the primary read/default-off concerns.

**The budget case is not uniformly steady-state fallback.** Its five primary values total 143 bytes; adding 4000 exceeds 4096 when all five are copied, as in ignore. In read-all, decoded stable known values can reuse strings after warmup without consuming the copy budget. The two unknown primary values total 50 bytes, leaving room for the unread 4000-byte X-Budget to defer. Read-all does not read X-Budget, so its much lower allocation rate is not evidence of cheap eager fallback. This exact frozen fixture/result is retained and disclosed, not silently changed or replaced. Exact-budget/fallback correctness is separately exercised in regression and socket tests.

## Local Crank reproduction

Frozen generalized shipping/workload source: **`96def7878b6e2ff7fb5f1afd73e54b4be35c103d`**. Baseline shipping source: **`2d105dd1fd119edff9a797a1b1aeca6d771c6134`**. Head Core SHA-256: `f486ecd632bb37ec9f4b7aa820493a2872e390d45054674acc4cfb3ca466496e`. [Complete per-file manifests](generalized/payload-manifests.json) prove that only Core DLL differs; identical head-side PDBs are retained but are not baseline debugging symbols. [Exact matched application/dependency binaries](generalized/matched-payloads.tar.gz) include repository license/notices. The original runtime remains a separate prerequisite.

At this frozen source, the broader functional selection again passes **777 + five skips**, and republished head/base/off/on payloads pass **96 real-socket requests** (12 processes, eight persistent requests each), asserting exact response body, observed character length and checksum, including missing/changed/known-duplicate/unknown-duplicate/budget/fragmented values. [Frozen-source logs and observations](generalized/frozen-source-verification.tar.gz). [Initial pilots, baseline timeout failure and traceback](generalized/initial-pilot-and-baseline-timeouts.tar.gz) are preserved separately.

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
python "$REPO/src/Servers/Kestrel/perf/LazyHeaders/generalized/run.py" \
  --controller "$CRANK_DLL" --repo "$REPO" --head "$HEAD_PUBLISH" \
  --base "$BASE_PUBLISH" --runtime "$POC_DOTNET_ROOT" --wrapper "$WRAPPER" \
  --source "$SOURCE" --output "$SUPPLEMENTAL_OUTPUT" --record-errors --repetitions 1 \
  --case read-one:changing --case enumerate:changing \
  --case ignore:duplicate-known --case read-all:duplicate-known \
  --case ignore:duplicate-unknown --case read-all:duplicate-unknown \
  --case ignore:budget --case read-all:budget
python "$REPO/src/Servers/Kestrel/perf/LazyHeaders/generalized/summarize.py" "$PRIMARY_OUTPUT"
python "$REPO/src/Servers/Kestrel/perf/LazyHeaders/generalized/summarize.py" \
  "$SUPPLEMENTAL_OUTPUT" --minimum-repetitions 1
```

The runner fixes 16 connections, two threads, 10-second timeout, 15-second warmup, 30-second measurement, pipeline one and five interleaved repetitions. It renders/asserts legacy `--application.source.localFolder` and `--load.source.localFolder` payloads, clears both `--load.source.project ""` and `--load.project ""`, and supplies **both** `pocDotnetRoot` and `wrkWrapperPublish`. Using plural `--application.sources.default.localFolder` can silently select the wrong source. Same short TMPDIR for agent/controller/application/load is required for EventPipe socket discovery.

Every run retains resolved config, exact command/timestamps, controller output, raw result/counter arrays, and actual uploaded Core/runtime identities. Inputs record full per-file payload, wrapper and shared-runtime manifests. `--resume` verifies source/settings and preserves interrupted directories; it never reruns/replaces completed cases. `--record-errors` retains socket errors, not bad responses or missing metrics. Missing metrics are not zero. No best-run selection or allocation-rate/total-request shortcut is used.

## Historical evidence

All prior microbenchmarks, socket checks, pilots, setup errors, interrupted runs, 60 primary User-Agent-only Crank runs and nine single-repeat supplemental cases remain unchanged under their original paths and commits. See [historical methodology](historical-user-agent.md), [raw archives and checksums](crank-local) and [original verification log](verification.txt). In particular the supplemental baseline enumeration's 15 timeouts and earlier default-off 16 timeouts are retained, not replaced or pooled into error-free primary results.
