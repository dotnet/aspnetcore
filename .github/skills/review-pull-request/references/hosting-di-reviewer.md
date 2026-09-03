### Hosting/DI reviewer

Review only ASP.NET Core hosting/DI work in `src/Hosting/**` and `src/DefaultBuilder/**`: generic host, `HostApplicationBuilder`, `WebApplicationBuilder`, `IHostBuilder`, compatibility-only `WebHostBuilder`/`IWebHostBuilder`/`IWebHost`/`WebHost` surfaces, service registration, options, startup, configuration, hosted services, lifetimes, scopes, and tests. `src/Extensions` is HTTP feature infrastructure (`src/Extensions/Features`) and belongs to servers-networking-reviewer, never here.

This file is reference material. The `review-pull-request` skill gives each dimension below an
independent, single-dimension pass.

#### Overarching principles

- Preserve host composition. Generic host, compatibility web host, minimal hosting, test host, slim-builder, and empty-builder paths must agree on shared configuration, services, lifecycle, and diagnostics unless a difference is deliberate and tested.
- Treat DI registrations as public contracts. Defaults must be idempotent, user-overridable where intended, and complete when default builders omit services.
- Make lifetimes explicit. Singleton, scoped, transient, hosted-service, options, change-token, and provider disposal choices must match ownership, thread-safety, and request/background boundaries.
- Keep startup ordering predictable. Configuration, service registration, startup filters, middleware, endpoint routing, `Build`, `StartAsync`, `Run`, and shutdown each have distinct responsibilities.
- Preserve trimming and NativeAOT viability. Builder paths, `UseStartup` discovery, DI activation, options binding, and startup diagnostics must avoid reflection assumptions unless the contract preserves the required members.
- Tests should prove user-observable host behavior across default, slim, empty-builder, compatibility web-host, generic, and test builders, not just helper implementation details.

#### Review dimensions

##### Scope, builder APIs, and compatibility

- CHECK: Keep `src/Hosting`, `src/DefaultBuilder`, and hosting-related `src/Extensions` code focused on host, builder, configuration, DI, options, startup, and lifecycle behavior; defer `src/Extensions/Features` HTTP feature infrastructure to [servers-networking-reviewer](servers-networking-reviewer.md) unless the change is a hosting integration boundary.
- CHECK: Public builder and service extension methods must keep conventional receiver types, chaining return values, overload naming, nullable annotations, XML docs, and API-review expectations.
- CHECK: Treat `WebHostBuilder`, `IWebHostBuilder`, `IWebHost`, and `WebHost` as obsolete ASPDEPR008 compatibility-only surfaces; frame new guidance around generic host, `HostApplicationBuilder`, and `WebApplicationBuilder` while preserving documented legacy behavior.
- CHECK: Maintain equivalent behavior between generic host, compatibility web host, minimal hosting, test host, default-builder, slim-builder, and empty-builder entry points when they expose the same setting, service, or lifecycle hook.
- CHECK: Make intentional divergences explicit in the API shape, error message, or test name so users can tell whether behavior differs by builder type.
- CHECK: Account for `CreateEmptyBuilder` and other empty-builder paths when comparing builder parity; deliberate exclusions such as server, routing, host filtering, and forwarded headers must remain explicit and covered by tests.
- CHECK: Keep the shared server-registration boundary clear: hosting owns host, builder, and provider lifecycle; [servers-networking-reviewer](servers-networking-reviewer.md) owns server and middleware feature registration plus required services.
- CHECK: Prefer current builder/configuration abstractions over legacy settings bags or post-build feature mutation for common host configuration.
- CHECK: Keep test-host and sample startup APIs as concise as production APIs; do not require users to add hidden default configuration or services for the common case.

##### Host and application configuration

- CHECK: Keep host configuration and application configuration precedence deterministic across JSON files, environment variables, command line, in-memory defaults, and explicitly supplied sources.
- CHECK: Synchronize `ApplicationName`, environment, content root, web root, and file providers between `HostBuilder`, `HostApplicationBuilder`, generic web host, and `IHostEnvironment`/`IWebHostEnvironment` so they cannot become split-brain.
- CHECK: Normalize content-root and web-root paths to stable absolute paths before consumers observe them; do not rely on current-directory or test-directory assumptions.
- CHECK: Keep `IConfiguration` and `IConfigurationSection` traversal semantics consistent from root to child sections; avoid casts or duplicate state that can drift.
- CHECK: Use `IFileProvider` abstractions for file-backed configuration and assets so custom providers, tests, and non-physical roots compose with defaults.
- CHECK: Configuration reload tokens can fire after provider `Set` calls even when effective values are unchanged; consumers must tolerate unchanged re-apply and compare effective values when change-sensitive work depends on it.

##### Service registration and idempotency

- CHECK: Use `TryAdd`, `TryAddEnumerable`, or an equivalent idempotent pattern for framework defaults, configurators, validators, and hosted services that should not duplicate or override user services on repeated `Add*` calls.
- CHECK: Use direct `Add*` registrations only when multiple implementations, ordering, or replacement is part of the contract, and test repeat-call behavior.
- CHECK: Feature entry points must register every dependency they require, including dependencies omitted by slim builders; do not rely on `CreateDefaultBuilder` side effects unless the API is scoped to that path.
- CHECK: Do not call `BuildServiceProvider` from `ConfigureServices`, builder extensions, or options setup to resolve services early; use factories, `IConfigureOptions`, validation, or host startup resolution instead.
- CHECK: Keep all registrations for the same service contract consistent across overloads, instance registrations, factory registrations, and implementation-type registrations.
- CHECK: Prefer constructor injection, `ActivatorUtilities`, or typed factories over static state and service-locator access; use `GetRequiredService` only where failure is part of the startup/runtime contract.

##### Service lifetimes and captive dependencies

- CHECK: Match singleton, scoped, and transient lifetimes to state ownership, thread-safety, disposal needs, and request/background boundaries.
- CHECK: Prevent captive dependencies: singleton services, hosted services, options objects, loggers, and caches must never capture scoped services; transient captures require ownership, disposal, and thread-safety review rather than a blanket ban (for example, container-owned `IStartupFilter` instances captured by hosted startup infrastructure can be valid).
- CHECK: Factories that receive `IServiceProvider` must be invoked with the correct root, request, or explicit scope; add tests when scope selection is observable.
- CHECK: Scoped services should represent request or operation state and be disposed with that scope; do not leak request services into application singletons or long-running callbacks.
- CHECK: Use instance/singleton registration only for prebuilt objects with clear ownership and disposal semantics; avoid mixing container-owned and externally owned lifetimes for the same object.
- CHECK: Keep stateless singleton caches and activators safe for concurrent use, and document only non-obvious lifetime or thread-safety invariants.

##### Provider, scope, and disposal lifecycle

- CHECK: Build the application `IServiceProvider` once per host and dispose it exactly once through host/application disposal, including `IAsyncDisposable` services.
- CHECK: Avoid replacing `ApplicationServices`, root providers, or request-services features after the host is built; expose service access through the established builder/application property names.
- CHECK: Create, restore, and dispose request or operation scopes deterministically, including error and early-return paths.
- CHECK: Dispose change-token subscriptions, file providers, hosted-service resources, loggers, streams, timers, and service scopes in an order that cannot observe partially stopped host state.
- CHECK: Validation options such as scope validation and build-time validation should surface developer errors without changing production defaults unexpectedly.
- CHECK: Startup and shutdown paths must not use disposed providers or resolve new services after the host has begun final disposal.

##### Options, validation, and change tracking

- CHECK: Prefer `IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`, `Configure`, `PostConfigure`, and validators over raw `IConfiguration` access in services that need typed host settings.
- CHECK: Use `IOptionsMonitor<T>` for live reload and dispose `OnChange` subscriptions; use `IOptionsSnapshot<T>` only in scoped request/operation flows.
- CHECK: Named options, default options, `ConfigureAll`, and `PostConfigure` ordering must compose predictably with user registration order and repeated `Add*` calls.
- CHECK: Validate required options at startup when invalid configuration should prevent serving; prefer contextual runtime exceptions only for rare paths that cannot be validated cheaply or safely up front.
- CHECK: Avoid broad open-generic options setup that applies configuration to unrelated option types; prefer concrete configurators for framework defaults.
- CHECK: Do not cache `IOptionsSnapshot<T>` or mutable options values in singletons; copy immutable data or use `IOptionsMonitor<T>` when the singleton observes changes.

##### Startup, middleware, and builder ordering

- CHECK: Keep service registration in service-configuration phases and middleware/endpoint setup in application-configuration phases; do not build request delegates or resolve application services prematurely.
- CHECK: Startup filters must compose with `WebApplicationBuilder`, `UseRouting`, `UseEndpoints`, and endpoint data sources without losing user middleware or route builders.
- CHECK: Default middleware such as exception handling, developer exception pages, routing, and endpoint execution should be added at the documented fallback point and remain user-overridable.
- CHECK: Distinguish build-time provider work from startup work: `Build` finalizes builder configuration and can run provider/`ValidateOnBuild` validation, while `StartAsync`, `Run`, and server start paths build the request pipeline, run startup filters, start hosted services, surface startup/runtime validation, and accept requests.
- CHECK: Default middleware injection should preserve routing order, rerouted branches, and opt-out invariants; authentication, authorization, and antiforgery defaults belong after routing when auto-applied.
- CHECK: `UseStartup` and startup-method discovery should produce actionable errors for missing, ambiguous, or invalid methods without obscure reflection or LINQ exceptions.
- CHECK: Builder APIs that expose server addresses, environment, configuration, or services should make values available before `Build` when user code can reasonably configure them there.

##### Hosted services, application lifetime, and graceful shutdown

- CHECK: `IHostedService` and `BackgroundService` start, stop, and exception behavior must follow `HostOptions`, application lifetime events, cancellation tokens, and service ordering.
- CHECK: Separate generic-host lifecycle rules (`HostOptions`, lifetime events, hosted-service ordering, and exception behavior) from compatibility web-host behavior such as `HostedServiceExecutor` and `WebHostOptions.ShutdownTimeout`.
- CHECK: Background-service exceptions should be observed, logged, and mapped to the configured host behavior; do not silently swallow failures or leave unobserved tasks running.
- CHECK: `StopAsync` and shutdown callbacks should honor cancellation, timeouts, and graceful SIGTERM/CTRL+C semantics, including successful zero-exit shutdown when appropriate.
- CHECK: Invoke `ApplicationStarted`, `ApplicationStopping`, and `ApplicationStopped` consistently even when cancellation callbacks throw; preserve useful exception context.
- CHECK: Prevent new requests or background work from entering a host that is stopping, and complete or cancel in-flight work through a documented path.
- CHECK: Avoid blocking waits or sync-over-async in start/stop paths that can deadlock hosted services, startup filters, or test hosts.

##### Environment, file providers, and static assets

- CHECK: Keep `IHostEnvironment` and `IWebHostEnvironment` complete before consumers observe them, including content root, web root, application name, environment name, and file providers.
- CHECK: Treat missing `wwwroot`, user secrets, static web assets, hosting-startup files, and configuration files as normal opt-in or fallback scenarios unless the feature explicitly requires the file.
- CHECK: Static web asset auto-loading has a two-stage shape: first development-gated, then manifest/configuration-driven; review both gates before treating asset behavior as unconditional.
- CHECK: Prefer standard environment variables and platform APIs for runtime, SDK, and path discovery; keep fallbacks deterministic and platform-aware.
- CHECK: Avoid keeping base paths or physical-file-provider state alive through statics except for deliberately scoped compatibility behavior.
- CHECK: Tests should not depend on ambient machine files, current directory, or absence of specific folders unless the scenario is explicitly about those inputs.

##### Trimming, AOT, security, and boundary safety

- CHECK: Keep builders, `UseStartup` discovery, DI activation, options binding/validation, and default, slim, and empty-builder paths safe for trimming and NativeAOT; avoid reflection-only contracts unless annotations, dynamic dependency declarations, or tests preserve the required members.
- CHECK: Treat configuration, user secrets, environment variables, command-line values, content roots, web roots, and file-provider paths as untrusted boundaries; validate or normalize paths before use and redact secrets or sensitive paths from logs, exceptions, and diagnostics.

##### Diagnostics, errors, logging, and tests

- CHECK: Error messages for missing services, invalid startup methods, invalid configuration, lifetime mismatches, and generic type failures should include the relevant service type, option type, key, or builder path.
- CHECK: Throw specific exception types where callers can act on them and preserve original exceptions when wrapping adds hosting context.
- CHECK: Logging should use structured fields, stable event IDs/categories, and `IsEnabled` guards for expensive loops; avoid noisy logs for normal fallback or opt-out paths.
- CHECK: Public APIs need XML documentation that explains purpose, return value, lifetime, and setup expectations without duplicating repository-wide guidance.
- CHECK: Add focused tests for repeated registration, user overrides, scoped factory providers, options reload/validation, configuration precedence, default versus slim versus empty builders, startup-filter ordering, hosted-service exceptions, graceful shutdown, disposal, and failure diagnostics.
- CHECK: Assert observable behavior such as services resolved, scopes disposed, configuration values, exception messages, log entries, lifetime events, exit codes, and request results; avoid assertions that only mirror helper implementation details.
