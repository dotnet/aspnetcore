### Cross-cutting reviewer

These dimensions apply to **every** ASP.NET Core change. The routing policy is explicit: this reviewer runs on **every** review, in addition to every routed domain reviewer, and it is **also** the primary reviewer for any `src` area that has no dedicated reference. It is never a fallback used only when no domain reviewer matched.

This file is reference material. The `review-pull-request` skill and `pull-request-review` workflow
give each dimension below an independent, single-dimension pass.

#### Overarching principles

- Preserve compatibility and public API discipline over local convenience. New APIs, constructors, options, packages, templates, analyzer IDs, and shared-framework metadata become long-lived contracts.
- Prefer existing ASP.NET Core, BCL, SDK, and repository infrastructure over custom helpers. Add abstractions only when they keep user code testable, composable, and stable.
- Treat nullability, validation, cancellation, disposal, and thread-safety annotations as executable design contracts, not comments.
- Separate startup, build-time, analyzer-time, and hot-path runtime work. Per-request, per-diagnostic, file-watcher, and template paths need allocation, caching, and determinism scrutiny.
- Treat filesystem paths, tool commands, configuration, generated output, logs, and package attribution as trust boundaries that can regress even when builds and tests pass.
- Tests should prove observable behavior across success, failure, edge, platform, and regression paths without relying on timing, working directories, external services, or implementation mirrors.

#### Review dimensions

##### Cross-cutting scope and change shape

- CHECK: Use this reviewer only for `src` areas without a more specific agent; route Components, gRPC, Minimal API/OpenAPI, MVC/Razor/routing, servers/networking, hosting/DI, SignalR, native interop, auth/security, WebTransport, project-file-only, and public API baseline work to their dedicated instructions.
- CHECK: Keep changes narrowly scoped to the affected product, tool, template, analyzer, or test utility; avoid unrelated file moves, generated-file churn, version churn, and broad refactors not required for the fix.
- CHECK: Preserve ownership boundaries between product code, shared source, templates, test infrastructure, and build infrastructure; do not expose shared implementation details through public namespaces or packages.
- CHECK: Prefer established shared helpers for process handling, retries, cancellation, file enumeration, CLI parsing, package metadata, and test hosting before adding one-off infrastructure.
- CHECK: Remove vestigial debug hooks, unused files, stale comments, obsolete workarounds, and duplicate conditional logic only once the scenario they protected is understood and still covered.

##### Public API surface, compatibility, and lifecycle

- CHECK: Minimize public surface area; keep speculative hooks, options, extension points, and constructor overloads internal until a demonstrated scenario and API review justify them.
- CHECK: Preserve public member names, constructor signatures, enum values, default option values, extension-method behavior, analyzer IDs, template identifiers, package identities, and shared-framework metadata unless the breaking change is deliberate and reviewed.
- CHECK: Use `[Obsolete]` with actionable migration guidance for deprecated APIs, and keep parallel or additive overloads when compatibility requires old members to remain.
- CHECK: Choose API shapes matching existing ASP.NET Core and .NET patterns: focused interfaces, explicit property names over ambiguous conversions, fluent extension methods that chain correctly, and abstract or virtual members only when inheritance is intentional.
- CHECK: Public XML docs must accurately describe purpose, parameters, return values, exceptions, defaults, lifecycle, and non-obvious examples for IntelliSense and generated docs.

##### Nullability, validation, and correctness invariants

- CHECK: Validate public API arguments and externally supplied data at entry points with precise exception types, parameter names, and actionable messages; do not let invalid input surface later as `NullReferenceException` or an ambiguous failure.
- CHECK: Keep nullable annotations, member initialization, `MemberNotNull`-style attributes, and guard clauses aligned with real control-flow invariants; do not use annotations to hide possible null states.
- CHECK: Model unknown, unsupported, or partially restored states explicitly instead of silently mapping them into an existing known bucket; keep classification separate from filtering so tests can prove both.
- CHECK: Preserve producer-consumer invariants for buffers, streams, collections, and generated metadata; never advance, reuse, mutate, or expose data beyond what the owner granted.
- CHECK: Use explicit comparers, culture rules, and case-sensitivity decisions for strings, keys, file names, template identifiers, and configuration names when default equality is not the contract.

##### Async, cancellation, and background work

- CHECK: Use Task-based async through the whole call chain; avoid `.Result`, `.Wait()`, sync-over-async wrappers, and synchronous exception behavior that differs from the async contract.
- CHECK: Flow `CancellationToken` to cancellation-aware async I/O, process execution, analyzer APIs, and long-running test utility APIs; for `IFileProvider.Watch` and file-change notifications, manage `IChangeToken` registration and disposable lifetimes through a documented shutdown path.
- CHECK: Use `ConfigureAwait(false)` in library code that should not capture a synchronization context, while preserving app/test patterns that intentionally rely on one.
- CHECK: Wrap fire-and-forget or callback-started async work in explicit error handling so failures are observed, logged, or propagated instead of crashing later or being silently swallowed.
- CHECK: Prefer `IAsyncDisposable` and async fixture/helper patterns when cleanup is asynchronous; do not block during teardown or replace cancellation with unsafe object nulling.

##### Performance, allocations, caching, and pooling

- CHECK: Determine whether code runs at startup, build time, analyzer time, or on a hot runtime path before raising performance findings; optimize hot paths and repeated analyzer traversals first.
- CHECK: Minimize avoidable allocations in per-request, per-diagnostic, file-provider, localization, cache, and template-generation paths using spans, pooled buffers, capacity hints, cached symbols, and lazy computation where they materially help.
- CHECK: Use object pooling, `ArrayPool<T>`, shared immutable singleton results, and cache consolidation only when ownership, reset, invalidation, contention, and lifetime semantics are clear.
- CHECK: Defer expensive or failure-prone work until a caller needs it; avoid constructor, registration, or startup work that eagerly resolves files, assemblies, projects, reflection, or external tools without benefit.
- CHECK: Measure durations with elapsed-time APIs and keep benchmark or performance validation focused on the changed path rather than wall-clock or environment-sensitive measurements.

##### Resource lifetime, disposal, and I/O

- CHECK: Types owning streams, file watchers, pooled buffers, processes, temporary artifacts, native handles, subscriptions, or listeners must make ownership explicit and release deterministically on success, failure, replacement, and cancellation paths.
- CHECK: Implement `IDisposable`, `IAsyncDisposable`, `SafeHandle`, or try/finally according to the resource owned; do not hide cleanup in unrelated lifecycle methods.
- CHECK: APIs that start registrations, watches, listeners, or background work must prevent repeated starts from leaking prior instances and define whether the caller or callee owns disposal.
- CHECK: Use `IFileProvider` for virtual/read-only file access, plus path APIs and platform helpers instead of hard-coded separators, working-directory assumptions, or string concatenation; use `System.IO` or explicit writable abstractions when code must create, write, move, or delete files.
- CHECK: Treat files, directories, generated outputs, and tool inputs as race-prone: they can be missing, locked, replaced, case-sensitive, URI-shaped, or deleted between an existence check and use.

##### Thread-safety, shared state, and lazy initialization

- CHECK: Protect shared mutable state with locks, immutable snapshots, concurrent collections, or one-time initialization that preserves the invariant, not just individual operations.
- CHECK: Document thread-safety guarantees for caches, options snapshots, pools, file providers, analyzers, and shared singleton services when callers can access them concurrently.
- CHECK: Do not dispose or null shared `CancellationTokenSource`, watcher, logger, cache, or pooled state while callbacks or lazy work may still observe it; cancel first and coordinate completion.
- CHECK: Keep lock scopes readable; avoid callbacks, logging fan-out, async continuations, or service resolution while holding a lock unless the reentrancy and deadlock behavior is intentional.
- CHECK: Use immutable or readonly fields for configuration and shared dependencies after construction; mutable static state needs a concurrency and test-isolation reason.

##### Options, configuration, and dependency injection

- CHECK: Use `IConfiguration`, options, and DI for configurable behavior; avoid hard-coded values, environment-specific branches, hidden mutable/environmental static dependencies, or service locators in product APIs, while allowing established factories and BCL shared pools such as `ObjectPool.Create<T>` and `ArrayPool<T>.Shared` when ownership and lifetime semantics are clear.
- CHECK: Keep configuration keys, option names, defaults, environment-variable mappings, generated element IDs, and casing stable across providers, and document them when users can depend on them.
- CHECK: Add configuration knobs only for demonstrated scenarios; choose safe defaults and validate options early enough that failures point to the invalid setting.
- CHECK: Match DI lifetimes to the dependencies they capture; use idempotent `TryAdd`/`TryAddEnumerable` registration when repeated calls should compose with user services.
- CHECK: Keep compile-time and runtime configuration distinct: MSBuild properties, runtime host configuration, package metadata, and user options must not drift or leak into the wrong layer.

##### Diagnostics, logging, exceptions, and tool output

- CHECK: Throw specific exceptions with contextual messages for invalid input, unsupported states, timeouts, external command failures, and configuration errors; catch only exceptions you can handle or enrich.
- CHECK: Keep diagnostics actionable — include the invalid value, path, key, package, tool, timeout, or operation where useful — without leaking sensitive data or stack traces in normal output.
- CHECK: Use structured `ILogger`, EventSource, analyzer diagnostics, and console output at levels matching success, warning, failure, and verbose detail; do not add noisy hot-path logs.
- CHECK: For tools and scripts, separate normal output from diagnostics, preserve readable ordering under parallel execution, handle no-op cases gracefully, and suppress presentation features when output is redirected.
- CHECK: Analyzer diagnostics need stable IDs, clear messages, help links, generated-code suppression where appropriate, and tests that verify locations and examples.

##### Trust boundaries, security, and sensitive data

- CHECK: Treat file-provider inputs, physical paths, generated outputs, and tool/configuration values as untrusted at boundaries; normalize and validate roots before opening files so traversal, symlink escape, URI confusion, or provider mismatch cannot bypass the intended scope.
- CHECK: Construct process, script, MSBuild, and external tool invocations with argument lists, response files, and repository helpers; do not concatenate untrusted values into shell commands, script fragments, or properties where command/process injection or quoting bugs can change behavior.
- CHECK: Redact secrets, connection strings, tokens, user secrets, credentials, sensitive environment values, and unnecessary local paths from logs, exceptions, diagnostics, generated files, and test artifacts.
- CHECK: Keep security-sensitive defaults fail-closed and preserve validation at trust boundaries even when the changed path is primarily configuration, templates, tests, or tooling.

##### Localization, text, paths, and cross-platform behavior

- CHECK: Externalize user-facing strings through resource/localization infrastructure and keep culture, UI culture, fallback, satellite-assembly, and formatting behavior explicit.
- CHECK: Use culture-aware or ordinal string APIs per the contract; avoid locale-specific assumptions in identifiers, paths, configuration keys, analyzer comparisons, and persisted values.
- CHECK: Handle text encoding, line endings, and persisted or protocol output deliberately, using canonical repo/protocol formatting instead of platform defaults when tools consume the output.
- CHECK: Detect operating system, architecture, target framework, SDK, and host capabilities through supported APIs or MSBuild metadata; avoid hard-coded platform configuration or unsupported path assumptions.
- CHECK: Keep platform-specific tests explicit with platform skip conditions, queue conditions, and separate scenarios rather than burying incompatible behavior in conditional test bodies.

##### Analyzers, reflection, source generation, trimming, and AOT

- CHECK: Analyzer logic may stay syntax-only when syntax fully proves the diagnostic; add semantic models, symbols, and related or multiple locations only when needed for correctness, symbol identity, partial declarations, or actionable fixes, and avoid false positives in generated code.
- CHECK: Cache repeated Roslyn symbol, type, and syntax lookups; combine tree traversal with diagnostic construction when it avoids redundant reflection or compilation work.
- CHECK: Keep analyzer and code-fix packaging lean; avoid heavy workspace dependencies in analyzer assemblies unless necessary for the shipped scenario.
- CHECK: Prefer explicit reflection lookups, generic constraints, source-generated metadata, and annotated APIs over broad reflection scans that are fragile under trimming or AOT.
- CHECK: Shared framework and template changes must stay trimming- and AOT-friendly; validate source-generation, `PublishTrimmed`, or `PublishAot` paths when behavior depends on metadata availability.

##### Build, packaging, shared framework, and templates

- CHECK: Preserve layouts, package metadata, target names, template identifiers, shared-framework inclusions, reference/runtime asset separation, and SDK/tooling compatibility that downstream consumers already load.
- CHECK: Keep shared build values single-sourced and narrowly scoped; prefer authoritative project metadata over duplicate opt-in properties, and avoid repository-wide imports or suppressions for local issues.
- CHECK: Update interrelated dependency versions, feed settings, source-commit metadata, and parent relationships as a coherent set; pin versions when reproducibility or host compatibility matters.
- CHECK: Validate MSBuild conditions, item metadata, glob depth, command-line escaping, response-file usage, and path normalization carefully, since small mistakes can silently disable build or packaging logic.
- CHECK: Dependency, package, or license changes must maintain third-party attribution artifacts such as `THIRD-PARTY-NOTICES.txt`; passing builds or tests can still hide a notice, license metadata, or attribution regression.
- CHECK: In templates and widely used assets, avoid speculative churn; reserve unique identifiers, keep generated output stable, and add behavior changes only for demonstrated scenarios with tests.

##### Tests, determinism, and test utilities

- CHECK: Add focused unit, integration, analyzer, template, or regression tests for changed behavior, covering success, failure, boundary, platform, and previously broken combinations.
- CHECK: Assert observable semantics — generated files, diagnostics, logs, configuration keys, exception messages, response headers, package metadata, and resource cleanup — rather than mirroring helper implementation details.
- CHECK: Keep tests deterministic: avoid timing-sensitive sleeps, current-working-directory assumptions, hard-coded ports, external service dependencies, order-sensitive output, and shared mutable state.
- CHECK: Test infrastructure should use shared helpers for unique paths, ports, retries, process execution, timeouts, output capture, Helix staging, and cleanup instead of duplicating per-test logic.
- CHECK: Preserve quarantine, skip conditions, pipeline conditions, and artifact staging until the protected scenario is understood; update the reason or implementation rather than removing the guard.
