# Historical User-Agent-only allocation experiment

This document and its measurements describe the superseded **User-Agent-only** POC through `112166c827fbc38e88f4327675964de15a4470d0`, not the generalized known/unknown header-value implementation. See [current status](README.md). None of these results measures the generalized implementation.

Discussion POC for [#69962](https://github.com/dotnet/aspnetcore/issues/69962), not a shipping optimization. **The 60-run local HTTP/1.1 Crank comparison is complete: allocation rate falls for changing unread values, but no trustworthy throughput improvement is established.** This shared client/server container cannot resolve small server-performance effects. Collection-level allocation results below remain diagnostic only.

**Current scope: HTTP/1.1 only.** The initial multi-protocol prototype and its raw measurements remain in history. The current implementation keeps HPACK/QPACK and HTTP/2/HTTP/3 literal-name ingestion eager even with the switch enabled. Their lazy-allocation experiments and performance comparisons are deferred; compatibility checks remain. A new HTTP/1 diagnostic comparison is recorded below, separately from the historical table.

## Hypothesis and alternatives

Deferring string creation for a changing header that an application never reads may reduce allocation pressure enough to improve a real workload. Stable known-header values already benefit from Kestrel's cross-request ASCII string reuse, so "fewer allocations than eager decoding" is not a sufficient hypothesis.

| Approach | Existing representation or tradeoff | Decision |
| --- | --- | --- |
| Existing known-header reuse | Generated bitsets and `StringValues`; compares ASCII bytes to a previous decoded value. Stable values can already allocate zero. | Keep as the baseline; preserve reuse after a lazy value is read. |
| Lazy values in owned storage | Copy validated bytes, decode when a value is exposed. Pays copying, validation, state, and first-read costs. | Prototype one known header, `User-Agent`, with at most 4 KiB of retained byte-array capacity per collection. |
| Borrow parser/HPACK/QPACK buffers | Avoids a copy but the parser and compression decoder may recycle input before application access. | Deferred; would require a separate lifetime/ownership design. This POC never retains those buffers. |
| Raw known-header representations | Response headers already have raw-byte setters for serialization. Request known-header names already use predefined names. | Reuse the generated lookup/bitset machinery, not the response serialization representation. |
| Lazy unknown names and values | Unknown headers use a case-insensitive string-keyed dictionary; delaying names requires changing lookup, duplicates, keys, and enumeration together. Known names already avoid per-request name creation. | Deferred to avoid conflating a dictionary redesign with the value experiment. |
| Declarative/opt-in materialization hints | Could target headers a workload ignores, but requires a consumer contract and must not mean silently dropping observable headers. | No existing hint contract found at these ingestion seams. Use an experimental process switch, not a new public API or an ignored-header declaration. |

Public correctness prior art: [pseudo-header reuse fix #38585](https://github.com/dotnet/aspnetcore/pull/38585) and [HTTP/3 stream/header reuse coverage #42681](https://github.com/dotnet/aspnetcore/pull/42681). These are correctness references, not evidence of a prior lazy-allocation win.

Related representation/API discussions: [#31492](https://github.com/dotnet/aspnetcore/issues/31492) weighs known-header fast-path coverage against per-collection memory; [#27467](https://github.com/dotnet/aspnetcore/issues/27467) concerns strongly typed header API consolidation, not a documented lazy-storage implementation. Neither supplies lazy-allocation performance evidence.

## Prototype boundaries

Set `Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyUserAgent` through `AppContext.SetSwitch` **before constructing Kestrel**. It is off by default and is not a supported product setting.

Only a first, nonempty, default-encoded ASCII `User-Agent` value of at most 4096 bytes is eligible. Validation still happens during append. Custom encoding selectors, non-ASCII values, oversized values, and duplicate appends go through the existing decoder/append path. A duplicate after an eligible first value materializes that first value and then appends normally.

The HTTP/1 parser callback enables the experiment only for HTTP/1.1. Shared named-header ingestion defaults to eager, so HTTP/2 and HTTP/3 literal names remain eager; HPACK index 58 and QPACK index 95 also retain their eager paths. A ref-return accessor centralizes materialization for existing getters, mutations, enumeration, and copies. Clear/reset can discard an unread HTTP/1.1 value without decoding it. Once decoded, an unchanged value can reuse the previous string. Shared collection layout/accessor changes still affect all protocols; unchanged ingestion is not a claim of zero performance impact.

Limitations to include in any interpretation:

- The reusable array retains previous bytes and up to 4 KiB of capacity for the collection's lifetime. Cold allocation, additional collection state, and connection/stream counts matter; these costs are not amortized by a cold-connection benchmark.
- Mutation through the ref accessor can materialize a value immediately before replacing/removing it. This POC prioritizes existing semantics over optimizing that case.
- The default-off path still has layout/accessor/branch changes. It is a required control, not an assumption of zero cost.
- `User-Agent` is one known header. This does not demonstrate the benefit of making every header lazy.
- The prepared socket/Crank application is HTTP/1.1 only. HTTP/2/HTTP/3 tests now check compatibility with the switch enabled, not lazy allocation. Their original diagnostic rows describe the superseded multi-protocol prototype.

## Verified correctness

See [verification.txt](verification.txt) for exact case identities and summaries.

- 940 targeted request-header cases pass, including 29 experiment/compatibility cases. Coverage includes owned input, case-insensitive lookup, duplicates, mutation/removal, enumeration/copy, reset with/without reuse, invalid input, custom encodings, and reuse of a decoded string.
- 3415 existing HTTP/1 connection/parser cases pass with no failures or skips after the HTTP/1.1 protocol gate was added.
- The current allocation check measures zero bytes for 100 warmed changing five-byte values on the explicitly enabled HTTP/1 path, but exactly 3200 bytes each for HPACK, QPACK, and default named-header ingestion with the switch enabled. This checks that compressed and literal-name HTTP/2/HTTP/3 paths remain eager. The original three-protocol eager-control experiment is historical, not current behavior.
- The process-isolated hosted HTTP/1 test passes through the actual parser, body consumption, and four keep-alive requests, including an unread value, a changed value, an absent value, and duplicates.
- Two additional process-isolated compatibility tests pass through HTTP/2 with `SocketsHttpHandler` over the existing in-memory transport and HTTP/3 with the existing multiplexed transport/QPACK harness while the process switch is enabled. These protocols no longer opt into lazy ingestion. Both consume request bodies before reading headers. HTTP/2 verifies four requests use one HTTP/2 connection; HTTP/3 verifies the same header collection is reused after stream disposal, including unread, changed, absent, and duplicate values. These three protocol tests are correctness checks, not performance or real-QUIC evidence.
- The broader HTTP/2/HTTP/3 functional selection passes with 775 passed, 5 skipped, and no failures, including the three opt-in cases. The existing tests exercise the default-off path. Four skips are Windows-version-specific HTTPS checks; the fifth requires TLS 1.1 ciphers that are disabled by default. This is functional regression evidence, not an end-to-end load test.
- The earlier multi-protocol prototype's published workload passed 24 real-socket checks across off/on and ignore/read/enumerate. The same host with only the Core DLL replaced by the baseline passed another 12 checks. [Historical head](workload-smoke.json), [baseline](baseline-workload-smoke.json), and [historical Core DLL hashes](kestrel-core-sha256.json) are retained. These do not verify a republished current-head payload.
- The narrowed HTTP/1.1 workload was subsequently republished and passes **24 current-head and 12 baseline-Core real-socket checks**, across ignore/read/enumerate and off/on. Each process serves four requests on the same persistent connection, checking changed/absent/duplicate values, exact response body and observed header lengths. Actual local Core loading was verified through process mappings. [Current checks and DLL hashes](http1-only/workload-smoke.json) identify the new head DLL separately from the historical payload. These are correctness checks, not load measurements.
- Earlier multi-protocol prototype full Core run: baseline 9900 passed / 13 failed / 1 skipped; prototype 9928 passed / 13 failed / 1 skipped. The same 13 IPv6-expectation cases fail on both. This environment disables IPv6; changing the process setting did not eliminate the failures. **The full suite was not green; these counts predate scope narrowing.**
- The Lua script was checked with Lua 5.4, including distinct fixed-width values on each of 16 simulated persistent connections, and subsequently executed through the real pinned Crank/wrk wrapper in the runs below.

Build/test entry points, from the repository root after the standard restore/bootstrap and `source activate.sh`:

```sh
dotnet test src/Servers/Kestrel/Core/test/Microsoft.AspNetCore.Server.Kestrel.Core.Tests.csproj \
  --no-restore -c Release --filter 'FullyQualifiedName~HttpRequestHeaders'
dotnet test src/Servers/Kestrel/test/InMemory.FunctionalTests/InMemory.FunctionalTests.csproj \
  --no-restore -c Release \
  --filter 'FullyQualifiedName~Http2|FullyQualifiedName~Http3|FullyQualifiedName~LazyRequestHeadersTests'
```

The initial bootstrap used `./eng/build.sh --restore --no-build --projects '<absolute Core/test/*.csproj>' /p:NuGetAudit=false` because audit metadata was unavailable. Dependency restore also needed invocation-only `--source` overrides for `https://api.nuget.org/v3/index.json`, `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet12/nuget/v3/index.json`, and `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json` after some public mirror downloads failed. Repository NuGet configuration and SDK pins were not changed.

## Diagnostic measurements

### HTTP/1-only revision

Measured prototype/fixture: `dff3b6a81dd5be2e7e76e706919293e76ef4d1df` (shipping code from `651a4e198ff654bcb19d3a81af23055f9503c97b`).
Unmodified shipping baseline: `2d105dd1fd119edff9a797a1b1aeca6d771c6134`.
Fixture SHA-256: `e8279f1ede552663f0ac97ad703e5087f644d81234d73d686d6a0bfb9baee1e9`.

Both successful runs contain 16 cases: off/on x stable/changing x ignore/read/enumerate/duplicate, HTTP/1 ingestion only. The baseline import compiles the same fixture with `LAZY_HEADERS_BASELINE` solely to omit the new opt-in argument against the old API. Baseline on-switch rows remain eager; compare against baseline off-switch rows.

| Values / access | Baseline ns | Head off ns | Head on ns | Bytes/request, base / off / on |
| --- | ---: | ---: | ---: | ---: |
| Stable / ignored | 16.20 | 19.20 | 24.80 | 0 / 0 / 0 |
| Stable / read | 15.61 | 18.92 | 25.81 | 0 / 0 / 0 |
| Stable / enumerate | 34.18 | 37.65 | 45.55 | 0 / 0 / 0 |
| Stable / duplicate | 81.30 | 86.38 | 93.20 | 176 / 176 / 176 |
| Changing / ignored | 45.59 | 47.68 | 25.45 | 88 / 88 / 0 |
| Changing / read | 44.76 | 47.54 | 50.65 | 88 / 88 / 88 |
| Changing / enumerate | 61.58 | 66.29 | 62.90 | 88 / 88 / 88 |
| Changing / duplicate | 82.43 | 89.08 | 91.99 | 176 / 176 / 176 |

[Head JSON](http1-only/head.full.json), [baseline JSON](http1-only/base.full.json), [32 derived rows with confidence margins](http1-only/measurements.csv), [head raw output](http1-only/head.raw.txt.gz), and [baseline raw output](http1-only/base.raw.txt.gz) preserve the new results and embedded environment. Release, BenchmarkDotNet 0.13.0, in-process emit, server GC, SDK `11.0.100-rc.1.26420.103`, runtime `12.0.0-alpha.1.26473.114`; 3 warmups, 8 measurement iterations at 100 ms, one launch, 6-8 retained measurements. Runs were sequential head then baseline on the same shared two-core AMD EPYC container, not interleaved or isolated.

The allocation mechanism still works for changing unread HTTP/1 values, but stable/read/default-off timings expose costs. These diagnostic rows do not establish controlled cross-run significance, acceptable regressions, or end-to-end improvement. The parser-version gate and networking are not exercised by this collection-level fixture. Use the reproduction commands below with the current fixture/import; Crank remains the required outcome measurement.

### Historical multi-protocol revision

**Historical multi-protocol results, not current-head measurements.** The HTTP/1.1-only revision changes the ingestion signature, protocol gate, and fixture call. Do not relabel the old results or DLL hashes as measurements of the current code. New HTTP/1.1 baseline/off/on Crank comparisons remain required.

Shipping baseline: `2d105dd1fd119edff9a797a1b1aeca6d771c6134`.
Prototype and benchmark fixture: `ca7c5fb527f3c01ed6728855e2d3b27d5a43705f`.
The fixture SHA-256 is `bf3d0c831612a287ffc82e212bc3757eeccd1a97232fb30b401a6282d91c3c94`.

The identical original fixture was compiled against an unmodified baseline Core using an extra compile-item import; no baseline shipping source was patched. Baseline `Lazy=true` rows still execute eager code because the switch does not exist there. Use baseline `Lazy=false` rows for the comparisons below. The current fixture selects only HTTP/1 and explicitly enables that path; the baseline import defines `LAZY_HEADERS_BASELINE` solely to omit the new argument against the old API. To reproduce the historical results, use the fixture and import from the recorded commits, not the revised fixture.

Both runs completed successfully, with 48 parameter combinations each. Run order was head, then baseline. BenchmarkDotNet 0.13.0; Release; in-process emit toolchain; .NET 12.0.0-alpha.1.26473.114 runtime; SDK 11.0.100-rc.1.26420.103; server GC; 3 warmup and 8 measurement iterations at 100 ms; one launch. Outlier filtering retains 6-8 measurement values per row. [Environment](environment.txt), [base JSON](base.full.json), [head JSON](head.full.json), [base raw output (gzip)](base.raw.txt.gz), [head raw output (gzip)](head.raw.txt.gz), and [derived CSV with confidence intervals](measurements.csv) are retained.

Selected **HTTP/1 microbenchmark means**, in ns/request:

| Values / access | Baseline | Head off | Head on | Bytes/request, base / off / on |
| --- | ---: | ---: | ---: | ---: |
| Stable / ignored | 16.13 | 18.61 | 24.52 | 0 / 0 / 0 |
| Stable / read | 15.77 | 18.06 | 25.48 | 0 / 0 / 0 |
| Stable / enumerate | 34.44 | 37.21 | 44.99 | 0 / 0 / 0 |
| Stable / duplicate | 87.09 | 84.85 | 89.56 | 176 / 176 / 176 |
| Changing / ignored | 44.74 | 46.14 | 24.78 | 88 / 88 / 0 |
| Changing / read | 45.26 | 46.86 | 55.76 | 88 / 88 / 88 |
| Changing / enumerate | 61.00 | 63.80 | 62.87 | 88 / 88 / 88 |
| Changing / duplicate | 81.48 | 85.47 | 91.91 | 176 / 176 / 176 |

This supports the narrow allocation mechanism and reveals read/default-off costs worth investigating. It does **not** establish a workload throughput improvement or an acceptable regression budget. This was a shared two-core container, with no machine isolation or randomized/interleaved launches. Do not treat the nanosecond differences or their within-run confidence intervals as controlled cross-run significance.

The first attempt combined an in-process job with the default out-of-process job; the latter rejected the SDK/target-framework combination. Those mixed-job results are not used here. The successful runs use the repository's `debug` configuration **in Release**, which selects an in-process job. It has no table column provider, so its summary CSV is empty; `measurements.csv` was derived from the complete successful JSON statistics. This configuration is diagnostic, not a substitute for the normal perf-lab toolchain.

Reproduce the head matrix:

```sh
source activate.sh
dotnet build src/Servers/Kestrel/perf/Microbenchmarks/Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks.csproj -c Release
dotnet run --no-build --no-restore -c Release \
  --project src/Servers/Kestrel/perf/Microbenchmarks/Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks.csproj -- \
  --config debug --filter '*LazyRequestHeadersBenchmark*' \
  --warmupCount 3 --iterationCount 8 --iterationTime 100 --launchCount 1 \
  --exporters json csv markdown --artifacts /absolute/path/to/head-results
```

For the baseline, use a separate checkout at the baseline SHA, perform its own normal bootstrap/restore, then compile the identical fixture:

```sh
dotnet build src/Servers/Kestrel/perf/Microbenchmarks/Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks.csproj \
  --no-restore -c Release \
  -p:CustomAfterMicrosoftCommonTargets="$HEAD_REPO/src/Servers/Kestrel/perf/LazyHeaders/baseline-harness.targets" \
  -p:LazyHeadersHarnessRoot="$HEAD_REPO"
```

Run the same `--no-build --no-restore` command from that checkout with a separate artifact directory. `HEAD_REPO` is the absolute path to the prototype checkout. The persisted import has the same compile-item behavior as the fixed-path session import used for the recorded run.

## Crank execution: completed local HTTP/1.1 comparison

All **60 primary runs** completed with positive requests/RPS/latency percentiles, application and load measurements, verified uploaded Core/runtime identities, and **zero bad responses or socket errors**. They served **70130567 measured requests** in total. Each stable/changing x ignore/read x baseline/off/on combination has five repetitions. Variants rotate by repetition; case ordering reverses on alternating repetitions. No best-run selection or outlier removal was applied.

Measured application shipping code: `651a4e198ff654bcb19d3a81af23055f9503c97b`; baseline shipping source: `2d105dd1fd119edff9a797a1b1aeca6d771c6134`. Identical Release-published host/dependencies and server GC were used, replacing only `Microsoft.AspNetCore.Server.Kestrel.Core.dll`. Loaded copies and the original `12.0.0-alpha.1.26473.114` runtime were checked for every run. [Complete payload manifests/runtime/GC/environment](crank-local/payload-environment.json) and [tool/source hashes](crank-local/identities.json) identify the experiment independently of later documentation commits.

Fixed primary settings: plaintext HTTP/1.1 GET `/`, keep-alive, pipeline=1, no request body, 44-byte `User-Agent`, 16 connections, 2 wrk threads, explicit 10-second timeout, 15-second warmup, 30-second measured load. The Lua sequence changes on every request in changing mode. Stable reuse, reads, and default-off controls are included; no cold-connection conclusion is supported.

Median RPS over five repetitions, with observed min-max:

| Access / values | Baseline | Head off | Head on |
| --- | ---: | ---: | ---: |
| Ignore / stable | 37912 (37386-39021) | 39419 (37350-39691) | 37948 (37024-39704) |
| Ignore / changing | 39214 (38512-41274) | 38275 (37771-39917) | 39059 (38130-39968) |
| Read / stable | 38682 (38325-41246) | 39346 (38236-44320) | 38586 (37698-40158) |
| Read / changing | 38628 (37142-39240) | 38635 (37729-40478) | 38876 (37798-39199) |

Median of each run's trimmed-window mean allocation rate, in decimal MB/second, followed by median p99 latency in ms:

| Access / values | Base alloc / p99 | Off alloc / p99 | On alloc / p99 |
| --- | ---: | ---: | ---: |
| Ignore / stable | 10.05 / 5.67 | 10.45 / 5.23 | 10.07 / 5.32 |
| Ignore / changing | 14.89 / 5.11 | 14.59 / 5.39 | 10.35 / 5.05 |
| Read / stable | 10.39 / 5.39 | 10.43 / 5.43 | 10.14 / 5.26 |
| Read / changing | 14.73 / 5.29 | 14.66 / 4.95 | 14.60 / 5.21 |

Allocation-rate reduction in changing/ignored is observed at roughly similar throughput; reads still require allocation and stable values already reuse strings. **This does not establish an overall throughput win or acceptable regression budget.** Paired on/base RPS differences range from -4.52% to +1.92% for changing/ignored and -3.48% to +1.99% for changing/read. Stable/read on/base median paired difference is -1.22%, with -2.64% to +0.68% spread. Do not interpret the median ratios or these intervals as controlled statistical significance. The earlier microbenchmarks' read/default-off costs remain relevant even when this noisy end-to-end run cannot isolate them.

CPU is a material limit: trimmed server process CPU averages about 110-114 core-percent and the wrk child about 48-52 core-percent, while agent/global overhead shares the same two reported processors. The client is not CPU-idle, and the experiment does not demonstrate a load-generator-independent server ceiling. Median server working set is roughly 71.5-72.5 MB; changing/ignored median Gen0 rates are base/off/on 5.75/5.625/4.0 per second. Coarse time-in-GC samples usually report zero despite nonzero collections: zero at this sampling resolution is not absence of GC.

Allocation/GC/CPU/working-set summaries use timestamped samples in a trimmed window from 27 to 3 seconds before the wrapper's first RPS result event, approximating the interior of the 30-second measurement. All runs retain 24 server allocation-rate samples in this window. This is an explicitly defined window, not a maximum over startup/warmup. No rate is divided by a total count or called bytes/request; no missing measurement is treated as zero. Raw timestamps allow other window choices.

[Per-run derived CSV](crank-local/primary-derived-runs.csv), [all median/min/max/stddev summaries](crank-local/primary-derived-summary.json), [exact run order](crank-local/primary-plan.json), and [all raw JSON/logs/resolved configurations/loaded identities and reproduction scripts](crank-local/primary-raw.tar.gz) are preserved. The raw archive also retains the interrupted attempt at run 54; runs 54-60 resumed after a session interruption, so the five repetitions are not one uninterrupted machine reservation. The first 53 successful runs were not repeated or replaced.

### Supplemental enumeration and duplicate cases

Nine additional cases used the same load settings, **one repetition each**, not a five-repeat performance comparison. Enumeration/changing baseline/off/on measured 37694/37453/37327 RPS; ignore/duplicate 38797/39969/38878; read/duplicate 37536/38707/39852. These single-run values are provided for transparency, not regression or improvement claims.

All nine have positive requests/latencies/application and load measurements and verified loaded identities. **The baseline enumeration run reported 15 timeout errors**; all other supplemental cases reported zero socket errors and all reported zero bad responses. The runner initially stopped on that baseline error, then continued the remaining predetermined cases in record-errors mode without rerunning or replacing it. Thus the supplemental set is not error-free and cannot be pooled into the primary zero-error comparison. No product-regression attribution is justified from these shared-host observations.

[Supplemental CSV](crank-local/supplemental-derived-runs.csv), [complete raw configurations/results/logs/identities](crank-local/supplemental-raw.tar.gz), and [artifact SHA-256 checksums](crank-local/artifact-sha256.txt) preserve every case. Supplemental measured requests total 10400644. The owned agent was stopped after confirming its job queue was empty; no server/load jobs were left running.

### Pilot and setup failures (not selected outcome results)

Pinned controller/agent/wrk-wrapper jobs have now run the actual published POC on its original net12 runtime. A short baseline changing/ignored feasibility run completed 178344 requests at 35653.49 requests/second, with positive latency percentiles, server/runtime counters, zero bad responses and zero socket errors. This is a five-second pilot, **not comparative outcome evidence**.

[Executable/source identities](crank-local/identities.json) and [all early attempt logs/configs/results](crank-local/pilots-and-initial-timeouts.tar.gz) are preserved. Early failures include the agent's inability to shallow-clone a commit as a branch, stale wrapper source-cache/project metadata, and a runner parser failure after the real successful pilot. The first full-duration matrix stopped after 16 timeout errors on its default-off case; that attempt remains included, not hidden.

The subsequent fixed-settings matrix above retains all 60 completed runs separately. Both processes share this two-core container; this cannot establish isolated-server capacity or small controlled performance gains. The explicit timeout change is a disclosed harness setting, not evidence that the original timeout event was a product defect.

The official wrk 4.2.0 commit `a211dd5a7050b1f9e8a9870b95513060e72ac4a0` was built locally with `make -j2` and frozen at the SHA-256 in the identities file. It is cached at `$TMPDIR/.crank/wrk-linux-amd64`; agent/application/load all use the same short `/tmp/lhp-7a143fff` temporary root, avoiding EventPipe Unix-socket path limits. The pinned wrapper is prepublished and uploaded too, avoiding the agent's unsupported shallow clone of a commit SHA and stale source-key cache.

Workload source: `8065742caba07399236ab62a5bc9db7c1424696e`. The workload/configuration are unchanged, but **republish the current HTTP/1.1-only Core** rather than reusing the old prototype payload or its DLL hash. [Configuration](../lazy-headers.crank.yml), [Lua workload](../lazy-headers.wrk.lua), and [application](../../samples/LazyHeadersApp) are included. The imported wrk job and its source are pinned to Crank `0d35cad5102892b091d9a88a52ea1f7a91bb1381`.

The application offers `ignore`, `read`, and `enumerate`. Every response includes `X-Observed-Length`, permitting a correctness smoke check before load. The Lua workload offers stable, changing, and duplicate values plus an unknown header and `Accept`. Changing mode uses a unique fixed-width sequence per wrk thread, not alternating values that could accidentally repeat on each connection. The 44-byte end-to-end `User-Agent` differs from the diagnostic fixture's value; compare each workload only against its own base/off/on controls.

Prepare two published folders with the identical host and dependencies; replace only the Core DLL for the baseline, as already smoke-tested locally:

```sh
source activate.sh
dotnet publish src/Servers/Kestrel/samples/LazyHeadersApp/LazyHeadersApp.csproj \
  -c Release -o "$HEAD_PUBLISH"
cp -a "$HEAD_PUBLISH" "$BASE_PUBLISH"
cp "$BASE_REPO/artifacts/bin/Microsoft.AspNetCore.Server.Kestrel.Core/Release/net12.0/Microsoft.AspNetCore.Server.Kestrel.Core.dll" \
  "$BASE_PUBLISH/Microsoft.AspNetCore.Server.Kestrel.Core.dll"
```

All variables above must be distinct absolute paths; `BASE_PUBLISH` must not already exist when copying. Record the Core DLL SHA-256 for both folders and verify the loaded server copy. The baseline checkout's Core must have been built in Release. This surgical DLL comparison isolates this PR's shipping-code change rather than silently replacing other runtime/framework components.

The Crank controller `0.2.0-alpha.26316.1` was installed locally. With `DOTNET_ROLL_FORWARD=Major` for that net8 controller on the available net10 host, its `--debug` mode successfully rendered the scenario, local payload, pinned script, and access/lazy/churn overrides. A deliberately unreachable loopback profile produced the expected endpoint warnings. **This verifies configuration rendering only, not agent upload, executable layout, wrk execution, counter collection, or throughput.**

The [loopback-poc.yml](loopback-poc.yml) profile runs both jobs through a local agent on `127.0.0.1:15010` and the HTTP/1.1 application on `127.0.0.1:15000`. Controller and agent source at Crank `0d35cad5102892b091d9a88a52ea1f7a91bb1381` build against existing SDK `10.0.402` / runtime `10.0.13`; the published POC stays on its original runtime, selected by `pocDotnetRoot`. The resolved payload path, server runtime root, client settings, and counter-provider list were checked for actual runs, not just configuration rendering.

Local runs share the two-core container with the load client and agent. They can establish reproducible end-to-end behavior and distributions, not isolated-server capacity or controlled lab significance. A separate-machine profile is useful for follow-up, not a prerequisite for attempting local evidence. Do not provision machines or alter lab infrastructure.

Set `AUTHORIZED_PROFILE_CONFIG` to this local profile and `AUTHORIZED_PROFILE=loopback-poc`. `POC_DOTNET_ROOT` is the original verified application runtime directory, not the controller's net10 installation. Publish `src/Microsoft.Crank.Jobs.Wrk/Microsoft.Crank.Jobs.Wrk.csproj` at the pinned Crank commit into `WRK_WRAPPER_PUBLISH`; use the existing net10 SDK/runtime and record the wrapper hash. The concrete local command is:

```sh
crank --config src/Servers/Kestrel/perf/lazy-headers.crank.yml \
  --config "$AUTHORIZED_PROFILE_CONFIG" --scenario lazy-headers --profile "$AUTHORIZED_PROFILE" \
  --application.source.localFolder "$HEAD_PUBLISH" \
  --load.source.localFolder "$WRK_WRAPPER_PUBLISH" --load.source.project "" --load.project "" \
  --variable pocDotnetRoot="$POC_DOTNET_ROOT" --variable wrkWrapperPublish="$WRK_WRAPPER_PUBLISH" \
  --variable access=ignore --variable lazy=true --variable headerMode=changing \
  --variable headerScript=https://raw.githubusercontent.com/dotnet/aspnetcore/8065742caba07399236ab62a5bc9db7c1424696e/src/Servers/Kestrel/perf/lazy-headers.wrk.lua \
  --load.variables.connections 16 --load.variables.threads 2 --load.variables.timeout 10 \
  --load.variables.warmup 15 --load.variables.duration 30 \
  --json head-on-ignore-changing.json
```

First use `--debug resolved.json` and assert that `Jobs.application.Sources.default.LocalFolder` equals the intended absolute payload directory; also inspect script, pinned versions, and endpoints. The pinned controller accepts the legacy `--application.source.localFolder` override for this YAML's `source:` shape. `--application.sources.default.localFolder` can silently retain the wrong configuration-relative folder and must not be used here. For the baseline use `BASE_PUBLISH` and `lazy=false`; for the default-off control use `HEAD_PUBLISH` and `lazy=false`. The configuration's branch URL is a convenience default only; the immutable script URL above is mandatory for recorded results.

Keep concurrency, request sizes, SDK/runtime, GC, machine configuration, and client revisions constant. The generic configuration starts with 256 connections / 16 threads, but the completed local comparison explicitly overrides these to 16 / 2 as above. Reproduce the exact 60-case local primary plan and rotating order using `reproduction/run-loopback.py` in the raw archive; it renders/asserts each payload, captures loaded identities, checks measurements and errors, and retains every run. Its recorded default paths must be replaced with the reproduction machine's corresponding absolute paths. Enumeration and duplicates are supplemental; no cold-connection conclusion can be drawn from this keep-alive workload. **Keep pipeline=1:** the imported wrk job selects a different script when pipeline exceeds one, which would invalidate this workload.

Before the controller, run the pinned agent with `TMPDIR=/tmp/lhp-7a143fff`, `--url http://127.0.0.1:15010 --hostname 127.0.0.1 --dotnethome "$NET10_ROOT" --build-path /tmp/lhp-7a143fff/jobs`. Verify `/jobs` responds and is empty. Use that same short `TMPDIR` for the controller and all jobs, and place the built official wrk binary at `/tmp/lhp-7a143fff/.crank/wrk-linux-amd64`. The local profile explicitly clears the imported wrapper source cache/repository/project metadata; without the legacy `--load.source.localFolder` override the controller may fail to upload the intended wrapper.

Retain resolved configuration, raw JSON and measurements, source/DLL identities, load and server CPU, RPS, latency distribution, bad-response counts, allocation rate, GC rate/time, and working set. Check that the client is not the bottleneck and report uncertainty rather than a single best run. The response-observation header and Lua formatting are workload overhead, not free instrumentation.

Do not divide a maximum runtime `alloc-rate` sample in bytes/second by a total request count and call it bytes/request. Preserve timestamped samples and align steady-state intervals/counts, or report allocation-rate distributions with their time window. Startup/warmup maxima are not measurement-window averages; missing metrics are not zero.

HTTP/2/HTTP/3 lazy-allocation implementations and end-to-end comparisons are deferred, not acceptance requirements for this HTTP/1.1 POC. The bounded implementation/evidence draft is delivered; design acceptance remains a discussion. Isolated client/server follow-up, cold/low-reuse memory behavior and a read/default-off regression budget are needed before a shipping recommendation. No merge or rollout is proposed.
