# API review conventions (ASP.NET Core)

The conventions the `@dotnet/aspnet-api-review` team applies. Each rule cites issue numbers you can open for the full discussion. Baseline is the [.NET Framework Design Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/); this catalog is the ASP.NET-Core-specific layer on top. When rules tension, **smaller public surface and consistency with existing APIs win.**

## Table of contents
- [Should this exist (need + audience)](#should-this-exist-need--audience)
- [Naming](#naming)
- [Namespaces & placement](#namespaces--placement)
- [Async & cancellation](#async--cancellation)
- [Nullability](#nullability)
- [Return & parameter types](#return--parameter-types)
- [Overloads vs optional parameters](#overloads-vs-optional-parameters)
- [Options & builders](#options--builders)
- [Extensibility & sealing](#extensibility--sealing)
- [Defaults](#defaults)
- [Consistency](#consistency)
- [Breaking changes & obsoletion](#breaking-changes--obsoletion)
- [Generics](#generics)
- [Ref-assembly hygiene](#ref-assembly-hygiene)

## Should this exist (need + audience)

- **Demonstrated need, not hypothetical.** Decline/defer when there's no real-world demand; "generic extensibility" is not a justification on its own (#2715, #48421). "uncommon enough that I don't think providing a first class option is necessary" (#48421).
- **Don't change an established idiom** unless the scenario is common enough to justify it (#39126).
- **Never add APIs that encourage anti-patterns**, even if technically possible — recommend the documented alternative or an event hook instead (#43138, #53712). Security-sensitive operations require explicit opt-in rather than a silencing convenience (#64154).
- **Intellisense pollution is a real cost.** Don't pile convenience members onto `HttpContext` or top-level `WebApplication`; prefer composition (an empty `MapGroup`) or endpoint extensions (#37425, #43237). Keep curated lists (e.g. `HeaderNames`) limited to commonly-used entries (#43013).
- **Keep separate concerns separate.** Don't fuse independently-configurable components without a strong composition reason (#42047); don't bolt auth onto session options (#51092); keep declarative and imperative option types apart (#65607).
- **Audience precedence:** end-app-developer happy path > library-author extensibility (needs a real scenario) > unjustified generic extensibility (decline). Scope domain APIs to common cases and point advanced users at libraries (#62287); gate dangerous-but-legitimate admin APIs behind capability flags + docs (#53502); when functionality already exists via a general mechanism, document that instead of adding a specific API (#45376).

## Naming

- **Avoid implementation words** in public names — no `Minimal`, no `Action` for route handlers (#35478). **Name the mechanism, not the intent:** `ServeMultithreadingHeaders` over `EnableMultithreading` (#54071).
- **Prefixes:** `Create*` for static factories (#42580); `Try*` for optional lookups — implies a nullable/`out` result and explicit fallback (`TryFindProperty`, #67080); `Set*` for replace-an-existing-key semantics (#24723).
- **Scope in the name:** when an extension lives on a shared builder but targets one server/feature, put it in the name (`UseKestrelHttpsConfiguration`, #47567); name endpoint-mapping helpers after the full API (`MapIdentity` → `MapIdentityApi`, #47227).
- **Drop redundant qualifiers** the containing type already implies (`PrimaryAuthenticationScheme` → `AuthenticationScheme`, #47232; `RemoveEnumTypePrefix` → `RemoveEnumPrefix`, #62891).
- **Positive booleans:** prefer `Allow*`/`Enable*` over `Disable*` to match existing metadata (#62883); don't embed `Unsafe` when the behavior is standards-compliant and documentable (#48461).
- **Match precedent:** `Noop` not `NoOp` (#47231); name a `TimeProvider` property `TimeProvider` (#47472); `Arguments` (what functions receive) vs `Parameters` (#40514); a string route property is `Route`, not `RoutePattern`, so callers don't expect the richer type (#47492).

## Namespaces & placement

- Niche/less-common types belong in a **feature sub-namespace**, not the root or a default global-using namespace (#46937, #47854). Concrete implementations live **beside their interface's namespace** (#47994). Built-in `IResult` types go in `Microsoft.AspNetCore.Http.HttpResults` (#47096).
- **Infrastructure** types not meant to be called externally go in a `*.Infrastructure` namespace (#62376, #62455). Group DTOs in a `*.Data` namespace and seal them (#50009, #49424).
- Consolidate `Add*` registrations into an existing well-known extension class rather than a new single-purpose one (#47228). Don't duplicate attributes across namespaces (#65066).

## Async & cancellation

- New async public methods return `Task`/`ValueTask` **with `CancellationToken cancellationToken = default`** and flow it through the whole chain; document what cancellation does (#66798, #45869, #61031).
- **`Task` vs `ValueTask`:** default to `Task`; use `ValueTask` only for request/response patterns with an often-synchronous path and a real justification — not for general callback delegates (#47937, #63181, #66798).
- Suffix async methods `Async` even when the paired delegate property uses `OnXxx` (#47937, #45874).

## Nullability

- **Required services**: prefer non-nullable properties that fail fast over nullable ones that silently allow misconfiguration (#47228).
- Use nullable to distinguish **unset vs set** (`IStatusCodeHttpResult.StatusCode`, #41919); absent context data is `nullable` + `init`-only when callers observe but shouldn't mutate it (#47651).
- Prefer non-nullable for new parameters unless `null` carries specific meaning (#53171); flip nullable→non-nullable when every usage requires a value (#42137).

## Return & parameter types

- **Inputs:** `IEnumerable<T>` for read-only sequences (#33700); `ReadOnlySpan<T>` for sync input, `Memory<T>` only when async requires it (#63181, #8606).
- **Returns/collections:** `IReadOnlyList<T>` by default; `ICollection<T>` when index access isn't needed (#47493); `IList<T>` only when callers must mutate/replace items (#40506); `IEnumerable<KeyValuePair<,>>` over `IReadOnlyDictionary` to avoid overload ambiguity (#41899).
- **Named result types:** replace anonymous `ValueTuple` returns with named `readonly struct`s for docs and discoverability (#46789); return a result struct instead of `bool` + `out` so `Try*` works with `ValueTask`/pattern matching (#63181). Return the **interface** type from `Try*` when a concrete type would force callers to suppress experimental diagnostics (#66955).
- Use an **options object** when a method's parameter count grows past a reasonable limit (#33700).

## Overloads vs optional parameters

- Prefer a **new overload** over adding/altering optional parameters when binary compatibility matters (#53171); but for one setting affecting many methods, a single configurable **property** beats adding a parameter to every overload (#47232).
- Always pair `Delegate` overloads with `RequestDelegate` overloads on `Map*` for consistency (#36198, #34450). Disambiguate delegate-type overloads with a name suffix (`AddEndpointFilterFactory`, #42589).
- Provide a **non-configuring** overload for the happy path alongside the `Action<TOptions>` one (#48531, #54600). Omit default values on new trim-safe dictionary overloads to avoid source-breaking overload resolution (#46542).

## Options & builders

- Wrap related callbacks/state in a dedicated **options class** for future extensibility (`TlsHandshakeCallbackOptions`, #33452); add a configuration surface (options parameter) to methods that currently have none when future per-mode customization is likely (#66829).
- Prefer **discoverable options-based configuration** over builder methods when it enables proper architectural separation (#42749). Use settable properties for boolean options over constructor params (#44286).
- Promote frequently-configured cross-cutting concerns (auth, etc.) to **first-class builder properties** (#39855, #42235).

## Extensibility & sealing

- **Seal by default** — concrete, metadata, options, and DTO types are sealed unless an extensibility scenario is justified; "seal all the things" (#49756, #17519, #37384); you can unseal later (#47994).
- **Marker interfaces** (no members) are fine when presence alone conveys meaning (#38573, #50057). Use **feature-detection interfaces** for protocol capabilities and typed rejection (#7801, #34824).
- Prefer an **abstract base class** over an interface only when you need default implementations + state (#54647); make context types `abstract` when subclassing is intended (#40514). For a new interface member not all implementors can satisfy, ship a **default interface member** (throwing or default-valued) rather than a required abstract member (#46538, #46823).
- Don't expose internals via `InternalsVisibleTo` for external extension — internal surface can change in servicing (#42317). Make a type public only when genuinely needed across assemblies (#48557).

## Defaults

- **Destructive operations default to off**, requiring explicit opt-in (#53880, #53502).
- Breaking a default behavior needs a major version + an escape hatch (transformers/options) (#64920).
- Don't register providers/features as defaults when they affect framework-wide behavior (#39010).

## Consistency

- Mirror **sibling APIs**: `IServiceCollection.Add*` + `With*` builders (#42667, #54600); new `IResult`/`TypedResults` implement `IEndpointMetadataProvider`/`IStatusCodeHttpResult` (#53073, #47096); match existing option/metric naming families (#46268, #47745).
- Use generic **`TBuilder`** on `IEndpointConventionBuilder` extension methods so they chain through `MapGroup` (#56039, #56178).
- Add `GetRequired*` variants beside optional `Get*` (#39921); provide both **attribute and extension-method** forms for endpoint metadata (#43222, #42667); add constructor overloads to match new method overloads (#56370).

## Breaking changes & obsoletion

- Prefer **non-breaking evolution**: add new (possibly virtual) members that delegate to the old, don't change return types/signatures in place (#60370, #53171).
- **Don't obsolete a working API** the moment a replacement lands — wait for evidence it causes problems; "we can always obsolete later" (#46575, #46542). Use `[Obsolete(..., error: false)]` until it's actually breaking in that version; include a message pointing to the replacement (#64860, #39382).
- Weigh an **analyzer** against a breaking interface change — often the analyzer wins (#52645). Add implicit converters to soften type replacements (#46157).

## Generics

- **Cap arity** at a pragmatic, framework-aligned limit (`Results<T1..TN>`, N up to 6–8) rather than every possible arity (#40672, #46676). Apply constraints (`where T : notnull`) to prevent misuse (#35849). Remove generic parameters that don't discriminate anything (#49404).

## Ref-assembly hygiene

- Proposals are reviewed in **ref-assembly format** (signatures only, no bodies/docs) — include public member attributes like `[Parameter]` so reviewers see the real shape (#46789, #47096). Annotate trimming attributes (`[DynamicallyAccessedMembers]`) and pair reflection/object-accepting overloads with trim/AOT warnings and a safe alternative (#24723, #47096).
