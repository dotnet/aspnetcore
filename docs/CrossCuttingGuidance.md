# Cross-cutting guidance

This guidance covers ASP.NET Core source work. Consult the relevant sections alongside the
requested task and applicable repository and area instructions.

## Overarching principles

- Preserve compatibility and public API discipline over local convenience. New APIs, constructors, options, packages, templates, analyzer IDs, and shared-framework metadata become long-lived contracts.
- Prefer existing ASP.NET Core, BCL, SDK, and repository infrastructure over custom helpers. Add abstractions only when they keep user code testable, composable, and stable.
- Treat nullability, validation, cancellation, disposal, and thread-safety annotations as executable design contracts, not comments.
- Separate startup, build-time, analyzer-time, and hot-path runtime work. Per-request, per-diagnostic, file-watcher, and template paths need allocation, caching, and determinism scrutiny.
- Treat filesystem paths, tool commands, configuration, generated output, logs, and package attribution as trust boundaries that can regress even when builds and tests pass.

## Topics

### Cross-cutting scope and change shape

- Apply the [contribution scope requirements](../CONTRIBUTING.md#before-submitting-the-pull-request) to product, tool, template, analyzer, and test-utility changes, including unrelated file moves, generated-file churn, version churn, and refactors beyond the affected scenario.
- Preserve ownership boundaries between product code, shared source, templates, test infrastructure, and build infrastructure; do not expose shared implementation details through public namespaces or packages.
- Prefer established shared helpers for process handling, retries, cancellation, file enumeration, CLI parsing, package metadata, and test hosting before adding one-off infrastructure.
- Remove vestigial debug hooks, unused files, stale comments, obsolete workarounds, and duplicate conditional logic only once the scenario they protected is understood and still covered.

### Public API surface, compatibility, and lifecycle

- Minimize public surface area; keep speculative hooks, options, extension points, and constructor overloads internal until a demonstrated scenario and API review justify them.
- Preserve public member names, constructor signatures, enum values, default option values, extension-method behavior, analyzer IDs, template identifiers, package identities, and shared-framework metadata unless the breaking change is deliberate and reviewed.
- Use `[Obsolete]` with actionable migration guidance for deprecated APIs, and keep parallel or additive overloads when compatibility requires old members to remain.
- Use applicable public API design criteria for changed public/protected APIs and shipped defaults/conventions. The [API review process](APIReviewProcess.md#process) governs that process; verify any concern against source signatures and contracts rather than reporting a design preference as a defect.
- Public XML docs must accurately describe purpose, parameters, return values, exceptions, defaults, consumer-observable lifecycle, and non-obvious examples for IntelliSense and generated docs; follow the repository's [public XML-documentation requirement](../.github/copilot-instructions.md#formatting).

### Nullability, validation, and correctness invariants

- Validate public API arguments and externally supplied data at entry points with precise exception types, parameter names, and actionable messages; do not let invalid input surface later as `NullReferenceException` or an ambiguous failure.
- Keep nullable annotations, member initialization, `MemberNotNull`-style attributes, and guard clauses aligned with real control-flow invariants; do not use annotations to hide possible null states.
- Model unknown, unsupported, or partially restored states explicitly instead of silently mapping them into an existing known bucket; keep classification separate from filtering so tests can prove both.
- Preserve producer-consumer invariants for buffers, streams, collections, and generated metadata; never advance, reuse, mutate, or expose data beyond what the owner granted.
- Use explicit comparers, culture rules, and case-sensitivity decisions for strings, keys, file names, template identifiers, and configuration names when default equality is not the contract.

### Async, cancellation, and background work

- Use Task-based async through the whole call chain; avoid `.Result`, `.Wait()`, sync-over-async wrappers, and synchronous exception behavior that differs from the async contract.
- Flow `CancellationToken` to cancellation-aware async I/O, process execution, analyzer APIs, and long-running test utility APIs; for `IFileProvider.Watch` and file-change notifications, manage `IChangeToken` registration and disposable lifetimes through a documented shutdown path.
- Use `ConfigureAwait(false)` in library code that should not capture a synchronization context, while preserving app/test patterns that intentionally rely on one.
- Wrap fire-and-forget or callback-started async work in explicit error handling so failures are observed, logged, or propagated instead of crashing later or being silently swallowed.
- Prefer `IAsyncDisposable` and async fixture/helper patterns when cleanup is asynchronous; do not block during teardown or replace cancellation with unsafe object nulling.

### Performance, allocations, caching, and pooling

- Determine whether code runs at startup, build time, analyzer time, or on a hot runtime path before raising performance findings; optimize hot paths and repeated analyzer traversals first.
- Minimize avoidable allocations in per-request, per-diagnostic, file-provider, localization, cache, and template-generation paths using spans, pooled buffers, capacity hints, cached symbols, and lazy computation where they materially help.
- Use object pooling, `ArrayPool<T>`, shared immutable singleton results, and cache consolidation only when ownership, reset, invalidation, contention, and lifetime semantics are clear.
- Defer expensive or failure-prone work until a caller needs it; avoid constructor, registration, or startup work that eagerly resolves files, assemblies, projects, reflection, or external tools without benefit.
- Measure durations with elapsed-time APIs and keep benchmark or performance validation focused on the changed path rather than wall-clock or environment-sensitive measurements.

### Resource lifetime, disposal, and I/O

- Types owning streams, file watchers, pooled buffers, processes, temporary artifacts, native handles, subscriptions, or listeners must make ownership explicit and release deterministically on success, failure, replacement, and cancellation paths.
- Implement `IDisposable`, `IAsyncDisposable`, `SafeHandle`, or try/finally according to the resource owned; do not hide cleanup in unrelated lifecycle methods.
- APIs that start registrations, watches, listeners, or background work must prevent repeated starts from leaking prior instances and define whether the caller or callee owns disposal.
- Use `IFileProvider` for virtual/read-only file access, plus path APIs and platform helpers instead of hard-coded separators, working-directory assumptions, or string concatenation; use `System.IO` or explicit writable abstractions when code must create, write, move, or delete files.
- Treat files, directories, generated outputs, and tool inputs as race-prone: they can be missing, locked, replaced, case-sensitive, URI-shaped, or deleted between an existence check and use.

### Thread-safety, shared state, and lazy initialization

- Protect shared mutable state with locks, immutable snapshots, concurrent collections, or one-time initialization that preserves the invariant, not just individual operations.
- Document thread-safety guarantees for caches, options snapshots, pools, file providers, analyzers, and shared singleton services when callers can access them concurrently.
- Do not dispose or null shared `CancellationTokenSource`, watcher, logger, cache, or pooled state while callbacks or lazy work may still observe it; cancel first and coordinate completion.
- Keep lock scopes readable; avoid callbacks, logging fan-out, async continuations, or service resolution while holding a lock unless the reentrancy and deadlock behavior is intentional.
- Use immutable or readonly fields for configuration and shared dependencies after construction; mutable static state needs a concurrency and test-isolation reason.

### Options, configuration, and dependency injection

- Use `IConfiguration`, options, and DI for configurable behavior; avoid hard-coded values, environment-specific branches, hidden mutable/environmental static dependencies, or service locators in product APIs, while allowing established factories and BCL shared pools such as `ObjectPool.Create<T>` and `ArrayPool<T>.Shared` when ownership and lifetime semantics are clear.
- Keep configuration keys, option names, defaults, environment-variable mappings, generated element IDs, and casing stable across providers, and document them when users can depend on them.
- Add configuration knobs only for demonstrated scenarios; choose safe defaults and validate options early enough that failures point to the invalid setting.
- Match DI lifetimes to the dependencies they capture; use idempotent `TryAdd`/`TryAddEnumerable` registration when repeated calls should compose with user services.
- Keep compile-time and runtime configuration distinct: MSBuild properties, runtime host configuration, package metadata, and user options must not drift or leak into the wrong layer.

### Diagnostics, logging, exceptions, and tool output

- Throw specific exceptions with contextual messages for invalid input, unsupported states, timeouts, external command failures, and configuration errors; catch only exceptions you can handle or enrich.
- Keep diagnostics actionable — include the invalid value, path, key, package, tool, timeout, or operation where useful — without leaking sensitive data or stack traces in normal output.
- Use structured `ILogger`, EventSource, analyzer diagnostics, and console output at levels matching success, warning, failure, and verbose detail; do not add noisy hot-path logs.
- For tools and scripts, separate normal output from diagnostics, preserve readable ordering under parallel execution, handle no-op cases gracefully, and suppress presentation features when output is redirected.
- Analyzer diagnostics need stable IDs, clear messages, help links, generated-code suppression where appropriate, and tests that verify locations and examples.

### Trust boundaries, security, and sensitive data

- Treat file-provider inputs, physical paths, generated outputs, and tool/configuration values as untrusted at boundaries; normalize and validate roots before opening files so traversal, symlink escape, URI confusion, or provider mismatch cannot bypass the intended scope.
- Construct process, script, MSBuild, and external tool invocations with argument lists, response files, and repository helpers; do not concatenate untrusted values into shell commands, script fragments, or properties where command/process injection or quoting bugs can change behavior.
- Redact secrets, connection strings, tokens, user secrets, credentials, sensitive environment values, and unnecessary local paths from logs, exceptions, diagnostics, generated files, and test artifacts.
- Keep security-sensitive defaults fail-closed and preserve validation at trust boundaries even when the changed path is primarily configuration, templates, tests, or tooling.

### Localization, text, paths, and cross-platform behavior

- Externalize user-facing strings through resource/localization infrastructure and keep culture, UI culture, fallback, satellite-assembly, and formatting behavior explicit.
- Use culture-aware or ordinal string APIs per the contract; avoid locale-specific assumptions in identifiers, paths, configuration keys, analyzer comparisons, and persisted values.
- Handle text encoding, line endings, and persisted or protocol output deliberately, using canonical repo/protocol formatting instead of platform defaults when tools consume the output.
- Detect operating system, architecture, target framework, SDK, and host capabilities through supported APIs or MSBuild metadata; avoid hard-coded platform configuration or unsupported path assumptions.
- Keep platform-specific tests explicit with platform skip conditions, queue conditions, and separate scenarios rather than burying incompatible behavior in conditional test bodies.

### Analyzers, reflection, source generation, trimming, and AOT

- Analyzer logic may stay syntax-only when syntax fully proves the diagnostic; add semantic models, symbols, and related or multiple locations only when needed for correctness, symbol identity, partial declarations, or actionable fixes, and avoid false positives in generated code.
- Cache repeated Roslyn symbol, type, and syntax lookups; combine tree traversal with diagnostic construction when it avoids redundant reflection or compilation work.
- Keep analyzer and code-fix packaging lean; avoid heavy workspace dependencies in analyzer assemblies unless necessary for the shipped scenario.
- Prefer explicit reflection lookups, generic constraints, source-generated metadata, and annotated APIs over broad reflection scans that are fragile under trimming or AOT.
- For shared-framework or template changes whose behavior depends on metadata availability, use the repository's [publish-time trimming and Native AOT validation procedure](Trimming.md#validate-trimming-behavior), including source-generated metadata paths.

### Build, packaging, shared framework, and templates

- Preserve layouts, package metadata, target names, template identifiers, shared-framework inclusions, reference/runtime asset separation, and SDK/tooling compatibility that downstream consumers already load.
- Keep shared build values single-sourced and narrowly scoped; prefer authoritative project metadata over duplicate opt-in properties, and avoid repository-wide imports or suppressions for local issues.
- Update interrelated dependency versions, feed settings, source-commit metadata, and parent relationships as a coherent set; pin versions when reproducibility or host compatibility matters.
- Validate MSBuild conditions, item metadata, glob depth, command-line escaping, response-file usage, and path normalization carefully, since small mistakes can silently disable build or packaging logic.
- Dependency, package, or license changes must maintain third-party attribution artifacts such as `THIRD-PARTY-NOTICES.txt`; passing builds or tests can still hide a notice, license metadata, or attribution regression.
- In templates and widely used assets, avoid speculative churn; reserve unique identifiers, keep generated output stable, and add behavior changes only for demonstrated scenarios with tests.

### Tests, determinism, and test utilities

- Apply the [repository test requirements](../CONTRIBUTING.md#tests) and [faithful-validation requirements](../.github/copilot-instructions.md#running-tests). Add focused unit, integration, analyzer, template, or regression tests for changed behavior, covering success, failure, boundary, platform, and previously broken combinations.
- Assert observable semantics — generated files, diagnostics, logs, configuration keys, exception messages, response headers, package metadata, and resource cleanup — rather than mirroring helper implementation details.
- Keep tests deterministic: avoid timing-sensitive sleeps, current-working-directory assumptions, hard-coded ports, external service dependencies, order-sensitive output, and shared mutable state.
- Test infrastructure should use shared helpers for unique paths, ports, retries, process execution, timeouts, output capture, Helix staging, and cleanup instead of duplicating per-test logic.
- Preserve quarantine, skip conditions, pipeline conditions, and artifact staging until the protected scenario is understood; update the reason or implementation rather than removing the guard.
