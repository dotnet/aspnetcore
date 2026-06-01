---
name: assessing-breaking-changes
description: 'Guides assessment of backward compatibility for aspnetcore changes. Consult when modifying public API, behavior, defaults, render-mode contracts, exception types, or output formats; when adding warnings; when deciding whether a change needs API review approval; or when reviewing the blast radius of behavioral changes. Particularly important for changes under Microsoft.AspNetCore.* public surface.'
argument-hint: 'Describe the change and its potential compatibility impact.'
---

# Backward Compatibility in dotnet/aspnetcore

Backward compatibility is the default — any change that could alter existing app behavior must be explicitly justified. This skill covers **how to evaluate compatibility risk** for aspnetcore changes.

## Core Philosophy

1. **Existing apps must not break.** Every minor/patch release upgrade must be a no-op for apps that don't opt into new behavior. Existing source code, configurations, and serialized state must continue to work.
2. **New warnings are breaking changes.** Apps and libraries that use `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` will fail if you introduce a new warning. Either gate new warnings behind an opt-in `AppContext` switch / configuration toggle, or emit them at `LogLevel.Information` instead.
3. **Public API additions require API review board approval.** Anything new under `Microsoft.AspNetCore.*`, `Microsoft.Extensions.*`, or `Microsoft.JSInterop.*` ships to every aspnetcore app on the next release. Public surface changes require approval from the API review board before merge — analyzer-suppressed or unshipped APIs are not exempt.
4. **Removal is nearly impossible.** Never remove public API members, extension methods, configuration keys, or default service registrations. Deprecate with `[Obsolete]` (with a diagnostic ID) first, then gate removal behind opt-in for multiple major releases.
5. **Behavioral changes are breaking even when signatures don't change.** A method that used to throw a different exception type, return a different value for the same input, or have different timing characteristics is a breaking change. Document and treat accordingly.
6. **Cross-render-mode parity is a compat dimension specific to Blazor.** A change that works under `InteractiveServer` but breaks `InteractiveWebAssembly` (or vice versa) is a breaking change for half the app surface even if the public API is unchanged.

## Blast Radius Checklist

Before merging any behavioral change, evaluate:

| Question | If Yes |
|----------|--------|
| Does this add a new `public` member to `Microsoft.AspNetCore.*`? | API review board approval required |
| Does this remove or change the signature of an existing public member? | Forbidden — deprecate with `[Obsolete]` instead |
| Does this change a default value (configuration, service registration, render mode, etc.)? | Opt-in switch required (`AppContext.SetSwitch` or config key) |
| Does this add a new warning (compiler, analyzer, runtime log at `Warning`+ level)? | Gate behind opt-in; consumers use `WarnAsError` |
| Does this change the type or message of an exception thrown by an existing API? | Behavioral breaking change — document; consider opt-in |
| Does this change behavior under one render mode but not another (Blazor)? | Document the divergence or apply uniformly |
| Does this change wire-format compatibility (SignalR protocols, auth cookies, antiforgery tokens, response shapes)? | Wire-compat impact — requires explicit cross-version testing |
| Does this affect trim-safety or NativeAOT compatibility? | Document trim/AOT impact; ensure annotations are correct |
| Does this affect only internal code paths with no user-visible effect? | No compat review needed |
| Is this a pure bug fix restoring documented behavior? | Usually no opt-in needed; use judgment on blast radius |

## When Opt-In Gating Is NOT Needed

- Internal refactoring with no observable behavior change
- Performance improvements that don't change semantics or timing-observable contracts
- New opt-in features gated by a new configuration key, service registration, or attribute
- Bug fixes that restore clearly-intended behavior with limited blast radius
- Test-only changes

## The Warnings-as-Errors Rule

This is the most commonly missed compatibility concern:

```xml
<!-- Many enterprise apps and most aspnetcore-consuming libraries set this -->
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

Any new warning you add (compiler diagnostic, analyzer diagnostic, runtime `ILogger` warning that a consumer might enforce) will **break these builds**. Options:

1. **Gate behind opt-in** — preferred for genuinely important warnings. Add an `AppContext.SetSwitch` or configuration key; default off.
2. **Use `LogLevel.Information`** — for informational diagnostics in runtime code paths.
3. **Add as an error from the start** — if the condition is always wrong and was never permitted.
4. **Add as analyzer with `[SuppressMessage]` defaults** — if it should warn, but defer enabling to a future version where it can be a default.

## Deprecation Protocol

1. Add `[Obsolete("Reason. Use X instead.", DiagnosticId = "ASPDEPRECATE_XYZ", UrlFormat = "https://aka.ms/aspnet-deprecate/{0}")]`. The DiagnosticId is mandatory — consumers need it to suppress individually.
2. Document the deprecation in the breaking-changes documentation and release notes.
3. Maintain the old behavior for **at least two major .NET versions** after deprecation.
4. Only remove after the deprecation has shipped through a full LTS cycle.

## Render-Mode Compatibility (Blazor-specific)

When changing code under `src/Components/**`, evaluate:

- Does the change behave identically under `InteractiveServer`, `InteractiveWebAssembly`, and `InteractiveAuto`?
- Does pre-rendering still work? Does the pre-render → interactive transition still produce no double-fetch?
- Does the change affect WASM trim/AOT safety? (Reflection use, dynamic code, large dependencies)
- Does the change affect circuit lifetime or memory growth under Server mode?

A change that's correct under one mode and broken under another is a breaking change for that mode's users.

## Wire-Compatibility (cross-version) Considerations

Some aspnetcore subsystems have **wire-compat contracts** that must survive across versions in mixed deployments:

- **SignalR protocols** (JSON, MessagePack) — message format must round-trip across client/server version skew
- **Data Protection** — keys persisted by older versions must decrypt under newer versions
- **Antiforgery tokens** — tokens issued by older versions must validate under newer
- **Authentication cookies** — cookies from older versions must authenticate under newer
- **Persistent component state** (Blazor) — state serialized by pre-render must hydrate under client interactive code

Changes touching these subsystems require explicit cross-version testing.

## Compatibility Test Matrix

When testing backward compatibility, verify:

- **Multi-targeting projects** — `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>`
- **Solution builds with mixed project types** — class libraries, web apps, Blazor (Server/WASM/Auto), Razor class libraries
- **WarnAsError builds** — explicitly test with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- **WASM trim safety** — `<IsTrimmable>true</IsTrimmable>` projects must not regress with new trim warnings
- **NativeAOT** — `<PublishAot>true</PublishAot>` projects must not regress
- **Mixed-version interop** for wire-compat subsystems above

## Decision Framework

```
Is the change user-visible?
├── No → Ship it (no compat review needed)
└── Yes
    ├── Is it a new opt-in feature (config key / attribute / service)? → Ship it (no compat review needed)
    └── Does it change existing behavior?
        ├── New public API?
        │   └── → API review board approval required, regardless of other answers
        ├── Bug fix with low blast radius? → Ship it, add regression test, document in release notes
        ├── Behavioral change or new warning?
        │   └── → Gate behind opt-in, test opt-out path
        └── Wire-format change (SignalR / DataProtection / Antiforgery / Auth / PersistentComponentState)?
            └── → Cross-version interop test required before merge
```

Document the compatibility decision in your PR description so reviewers can validate it. PRs that change public API or behavior **without** an explicit compat assessment in the description will be flagged by `blazor-expert-reviewer` (or other area reviewer) as needing one.

## Related skills and references

- `evaluate-pr-tests` — validates that the regression tests accompanying a fix actually exercise the bug
- `verify-tests-fail-without-fix` — proves the new test would catch the regression
- `blazor-expert-reviewer` agent — applies render-mode parity and trim-safety checks
- `code-review` skill — general aspnetcore review entry point

For specific label names, API review board ceremony details, and the current breaking-change tracking template, defer to the maintainer team — these evolve.
