### Native interop reviewer

Review only the ASP.NET Core native interop area in `src/Servers/IIS/**` and related Windows installer work in `src/Installers/**`: ANCMV2 (`aspnetcorev2.dll`; legacy `aspnetcore.dll` compatibility only), request handlers, forwarders, in-process and out-of-process IIS hosting, shim/hostfxr loading, managed P/Invoke layers, unmanaged resource lifetime, and IIS-native request-semantics tests.

This file is reference material. The `review-pull-request` skill gives each dimension below an
independent, single-dimension pass.

#### Overarching principles

- Preserve IIS and ANCM hosting semantics over local simplification. In-process, out-of-process, IIS Express, app-pool recycle, and `app_offline.htm` paths have different lifecycle contracts.
- Treat the managed/native boundary as an ownership contract. Handles, buffers, callbacks, `GCHandle`s, strings, and structs need explicit lifetime, marshaling, and error-propagation rules.
- Synchronize every state transition that can race with callbacks, async I/O, shutdown, or request completion. Avoid time-of-check-to-time-of-use gaps by capturing and re-checking shared state under the right primitive.
- Propagate native failures with actionable context. Preserve Win32 error codes and HRESULTs, map exceptions deliberately, and keep event logs/stdout diagnostics useful without noisy hot-path logging.
- Tests should exercise observable IIS behavior across process, hosting-model, architecture, and Helix boundaries, not just helper methods.

#### Review dimensions

##### Scope, hosting model, and ownership

- CHECK: Keep ANCM, IIS hosting, request-handler, shim/hostfxr, and installer changes within their current ownership boundaries; do not move unrelated server or runtime behavior into the IIS area.
- CHECK: Make in-process and out-of-process behavior explicit whenever app lifetime, process lifetime, request dispatch, shutdown, recycling, error reporting, or tests differ.
- CHECK: Validate hosting model configuration at the point it is consumed and prevent incompatible hosting models from sharing app-pool state.
- CHECK: Query the authoritative configuration object instead of passing duplicate hosting-model or path state that can drift across native and managed layers.
- CHECK: Keep IIS-specific managed APIs narrow and discoverable only where intended; avoid expanding public surface area for low-usage native features without API-review justification.

##### P/Invoke, marshaling, and ABI contracts

- CHECK: Keep managed P/Invoke signatures, native exports, calling conventions, HRESULT returns, `SetLastError` usage, and callback delegate shapes synchronized with the native implementation.
- CHECK: Prefer simple POD/blittable structs and explicit marshaling attributes across native seams; avoid passing STL containers or ambiguous layout through module boundaries.
- CHECK: Define ownership transfer for strings, buffers, handles, callback contexts, and `APPLICATION_PARAMETER`-style arrays; document who allocates, frees, pins, and may retain each value.
- CHECK: Check raw `IntPtr` values before `GCHandle.FromIntPtr()`, prevent callbacks from racing `Free()`, handle `.Target` null/failure, and order callback quiescence before freeing to avoid handle-slot reuse.
- CHECK: Keep nullable reference type annotations and contracts accurate on the managed P/Invoke surface separately from unmanaged pointer validation; avoid broad suppressions that hide real nullability bugs.
- CHECK: Match `SafeHandle`, `HandleWrapper`, `CComPtr`, or equivalent wrapper behavior to actual ownership, including borrowed handles and completion-signal/lifetime-barrier handles whose `ReleaseHandle()` does not call `CloseHandle`; encode why so cleanup is deterministic.
- CHECK: Keep callback parameters inline unless their lifetime is explicitly extended; do not store delegates, function pointers, or unmanaged context pointers without reference-counting and shutdown protection.
- CHECK: Treat P/Invoke signatures, marshaling attributes, hostfxr/native loading, callbacks, and managed interop layers as trimming/AOT-sensitive; preserve required entry points and metadata deliberately.

##### Handle, memory, and resource lifetime

- CHECK: Use RAII and smart pointers (`std::unique_ptr`, `std::make_unique`, wrapper objects) before ownership transfer; raw pointers are acceptable for explicit IIS/COM intrusive ref-counting or ownership-returning out-params when transfer and release points are documented.
- CHECK: Use the correct sentinel for each Windows handle kind (`NULL` versus `INVALID_HANDLE_VALUE`) and check it consistently before close, wait, duplication, or status operations.
- CHECK: Pair every allocation, pin, reference, file handle, pipe, logger, certificate, module handle, and temporary test resource with deterministic cleanup on success, failure, exception, and early-return paths.
- CHECK: Separate explicit `Shutdown()`/`Stop()` behavior from destructors so graceful recycle, abrupt unload, and test cleanup can coordinate managed and native state safely.
- CHECK: Cache expensive or one-shot native data such as SSL certificates or server-variable capabilities only when the value is immutable for the request/application lifetime and disposal remains deterministic.

##### Concurrency, locking, and TOCTOU prevention

- CHECK: Protect shared mutable state with the appropriate primitive: SRW locks for guarded state, `Interlocked`/`Volatile` for atomic flags and counters, and concurrent containers only when they preserve required invariants.
- CHECK: Capture pointers, handles, callbacks, and state flags into locals before validation and invocation, then re-check them under an exclusive lock when another thread can recycle, abort, clear, or transition the state; avoid lock-free shortcuts unless the state is purely advisory.
- CHECK: Avoid non-reentrant SRW-lock deadlocks by preventing nested acquisition, lock upgrades, recursive `Stop()`/shutdown calls, and callbacks into code that needs the held lock.
- CHECK: Track outstanding requests and async operations atomically, and signal shutdown only after in-flight work has completed or been cancelled through a documented path.
- CHECK: Initialize synchronization primitives before any code can observe them, and centralize one-time process-wide state behind a process-wide lock/atomic protocol, `InitOnceExecuteOnce`, or `std::call_once`; `volatile` alone is insufficient.
- CHECK: Protect `ValueTaskSource` or `ManualResetValueTaskSourceCore` continuation assignment with `Interlocked`, store struct values in fields instead of capturing struct properties in lambdas, and use `Volatile`/`Interlocked` for shared async I/O state to avoid lost or double-invoked continuations.

##### IIS request, stream, and callback lifecycle

- CHECK: Coordinate request start, completion, abort, disconnect, and native callback paths so request counts, `GCHandle`s, managed context pointers, and native context objects are released exactly once.
- CHECK: Respect IIS-native request semantics for `CompleteAsync`, request abortion, disconnect notifications, response trailers, WebSocket full-duplex mode, buffering toggles, and stream reset ordering; defer managed HttpSys server behavior to the servers networking reviewer.
- CHECK: Serialize IIS async read/write operations where the native API allows only one active operation, and cancel pending I/O before replacing or completing a request path.
- CHECK: Validate stream state transitions explicitly; closed or errored streams should follow .NET stream contracts and map native I/O failures to appropriate managed exceptions.
- CHECK: Keep server variables, Windows identity, client certificates, and header/trailer marshaling null-safe, encoding-aware, and aligned with IIS availability constraints.
- CHECK: Avoid writable feature fields for server-owned request/response resources unless mutation semantics are intentionally constrained and tested.

##### Startup, shutdown, recycling, and process management

- CHECK: Preserve app-pool recycle, `app_offline.htm`, IIS Express, graceful shutdown, startup suspension, and worker-process timeout behavior without exposing stale configuration or accepting requests into a stopping app.
- CHECK: Guard application creation and recreation with synchronization so concurrent first requests, recycle notifications, and hosting-model switches cannot create duplicate or dangling applications.
- CHECK: Stop managed applications and out-of-process worker processes deterministically with documented timeouts before replacing handlers or returning from shutdown paths.
- CHECK: Discover `dotnet.exe`, hostfxr, native request-handler DLLs, framework versions, and bitness-sensitive paths through platform APIs and repository-approved fallback paths.
- CHECK: Treat stdout/stderr redirection, startup error serialization, and initialization logging as best-effort diagnostics that clean up handles and empty files without blocking successful startup.

##### Error propagation, HRESULTs, and diagnostics

- CHECK: Capture `GetLastError()` immediately after failed Win32 calls and convert with `HRESULT_FROM_WIN32`; do not overwrite native error context before logging or returning.
- CHECK: Return, translate, and log semantically precise HRESULTs and Windows error codes; handle expected values such as cancellation, `S_FALSE`, sharing violations, and buffer-too-small conditions without spurious failures.
- CHECK: Catch C++ and CLR exceptions at native/managed boundaries and module entry points, then map them to HRESULTs, HTTP 5xx responses, or managed exceptions with preserved diagnostic context.
- CHECK: Use event log and debug logging consistently: include relevant path, process, version, HRESULT/error code, retry, and configuration context while avoiding verbose hot-path trace noise.
- CHECK: Use debug assertions after reference counting, pointer checks, and impossible state transitions to catch native invariant violations without replacing runtime error handling.

##### Strings, paths, buffers, and filesystem operations

- CHECK: Use current native string and path abstractions consistently for new code; avoid lossy conversions between managed strings, C strings, STRU/STRA, `std::string`, and `std::wstring`.
- CHECK: Validate buffer sizes before allocation or copy, reserve space for null terminators explicitly, and retry with larger buffers only through overflow-safe patterns.
- CHECK: Review native request-handler and managed/native marshaling hot paths for avoidable per-request allocations, transcoding, pinning, delegate creation, and buffer copies; cache only when lifetime and ownership remain correct.
- CHECK: Convert and validate paths with filesystem/path APIs rather than string concatenation; account for bitness-aware environment variables, absolute paths, access checks, and regular-file requirements.
- CHECK: Treat `app_offline.htm`, configuration files, logs, and installer inputs as files that can be locked, replaced, deleted, or changed between checks; design existence checks and watchers for races.
- CHECK: Respect native API limits such as chunk counts, header/trailer constraints, fixed-size stack strings, and architecture-dependent pointer alignment.

##### Installer, packaging, build, and architecture integration

- CHECK: Keep Windows installer projects, WiX references, native projects, forwarders, and shared framework bundles architecture-aware for x86, x64, ARM64, ARM64X, and ARM64EC, including ARM64X forwarding and side-by-side x64/ARM64 payloads where packaging requires them.
- CHECK: Use project references, shared MSBuild settings, and repository build metadata; reject duplicated literal artifact paths not backed by shared properties, while allowing explicit property-driven cross-arch/ARM64X payload paths.
- CHECK: Version ANCM, native binaries, file revisions, resources, and package metadata deterministically and independently where product semantics require it.
- CHECK: Align native toolset, VCTools, platform names, library paths, and installer versions across all affected projects; document constraints only when they are not already expressed in build files.
- CHECK: Keep package metadata, license/document URLs, schema defaults, comments, and file-layout changes accurate without duplicating repository-wide build guidance.

##### Tests, Helix assets, and cross-process validation

- CHECK: Add focused tests for changed IIS behavior across in-process and out-of-process hosting, including startup, shutdown, recycle, `app_offline.htm`, request lifetime, trailers, WebSockets, buffering, identity, certificates, and error paths.
- CHECK: Make IIS and IIS Express tests elevation-aware, OS-version-aware, port-safe, architecture-aware, and isolated from shared machine state through unique names and cleanup.
- CHECK: Test native resource release by deleting or reopening files, handles, directories, logs, and process outputs after the scenario completes.
- CHECK: Cross-process test assets must run locally and on Helix: discover payload-relative paths, capture logs under artifacts, enforce timeouts, validate process exit codes, and clean temporary outputs.
- CHECK: Prefer assertions over observable responses, headers, trailers, logs, HRESULTs, exit codes, and resource cleanup rather than assertions that mirror helper implementation details.
- CHECK: Reject unbounded or flaky local repro loops, redundant artifact generation, and order-sensitive assertions over external tool output unless order is part of the contract; deterministic bounded IIS stress tests are valid regression coverage.
