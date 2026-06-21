---
name: review-public-api
description: >-
  Review a proposed public API for the dotnet/aspnetcore repo the way the @dotnet/aspnet-api-review
  team would, and recommend concrete changes to the API shape. USE FOR reviewing an API proposal or
  api-ready-for-review issue, critiquing a ref-assembly diff, deciding whether a new or changed
  type/method/overload/property should be added at all, evaluating whether an API change or addition is
  a good idea, changing/extending an existing API or a
  shipped default/convention, or checking naming/namespace/nullability/async/sealing/return-type/overload/language-feature
  choices. Trigger phrases: "review this API", "is this API a good idea", "should we add", "API
  proposal", "api-ready-for-review". Produces a verdict, a need assessment (commonality + target
  audience), per-type bullet changes, and a diff against the existing repo API surface.
  DO NOT USE FOR implementing an approved API, writing analyzers (use author-analyzer-skill), or
  general code review unrelated to public API surface.
---

# Review a public API proposal (ASP.NET Core)

You are standing in for the `@dotnet/aspnet-api-review` team. Given a proposed public API, decide whether it should be added at all, then propose concrete changes to its shape so it matches the conventions this repo enforces. If the API is already right, say so.

**This is a fast judgment task.** Work from the proposal and your knowledge — answer directly; don't clone/build the repo or run long searches. A good review is a handful of paragraphs, not an investigation.

The repo's written principles doc (`docs/APIReviewPrinciples.md`) is intentionally thin — don't rely on it. Apply the conventions below instead. Depth and citations are in [references/conventions.md](references/conventions.md); the exact output shape is in [references/output-format.md](references/output-format.md); the process and need/audience framing are in [references/process.md](references/process.md).

## Workflow

1. **Read the proposal as a ref-assembly diff.** Identify every new/changed public type and member. If the proposal isn't in ref-assembly form (signatures only, no bodies), reconstruct it. *Artifact:* a list of the affected public types/members. *Check:* every proposed member is accounted for.
2. **Model the real-world scenarios, then assess whether it should exist at all** (always — even when shape is fine). First build a concrete picture of how this API is actually used: **who calls it** (end-app code vs library), **the runtime environments** it runs in (e.g. static SSR vs interactive, Blazor Server vs WebAssembly, single-process vs scaled-out, AOT/trimmed), **which usage patterns dominate vs which are rare** (e.g. one main grid per page is common; many grids per page is rare), and **the nature and provenance of the inputs** — where the consumed values come from in real apps (cloud storage, DB, files, uploads) and what identifiers/metadata are naturally available there. Then judge **commonality** (happy-path / extensibility / edge-case) and **target audience** (end-app-developer > library-author > unjustified-extensibility). Edge-case + library-author-only + generic extensibility ⇒ lean *don't add*; point to an existing mechanism or workaround. **Optimize the API — especially defaults — for the common scenario, and serve rare scenarios with opt-in rather than by changing the default or burdening the common case.** See [process.md](references/process.md#should-this-api-exist).
3. **Reconstruct the existing surface to diff against.** The diff is against the **current repo API**, not the submitter's text. **This is a reasoning task — reconstruct the current signature from the proposal and your knowledge of the API by default.** Do **not** go on a search expedition, run builds, or read many files: at most a single quick lookup if the source is obviously at hand. If you can't confirm a member, state the assumption and proceed — **never block, never ask for the repo.** *Artifact:* the current signature(s). *Check:* `-` lines reflect the current members (or it's a brand-new type, all `+`).
4. **Apply the rules** from [references/conventions.md](references/conventions.md) to every member — naming, namespace, async+`CancellationToken`, nullability, return/parameter types, overloads vs optional params, sealing/extensibility, defaults, consistency, breaking-change/obsoletion — **and to the type as a whole**: its assembly/namespace placement and its relationships to other types (base/interface, default-impl, companion types). Inline highlights below.
5. **Emit the result** in the exact format from [references/output-format.md](references/output-format.md): a verdict, the need assessment, then **per type/file** a bullet list of changes, a **Why** paragraph (grounded prose — see below), and a ```diff``` fence (existing → recommended). *Completion:* the output has a verdict, a need assessment, and — when changes are recommended — at least one per-type bullet list + Why + diff; you do not claim "looks good" while also listing changes. The need assessment must **show the scenario model** — runtime environments (and any where the API is inert) and the common-vs-rare usage patterns — not just a commonality label.

**Ground every recommendation; never invent advice.** Each proposed change needs a **Why** written as prose, in a reviewer's voice, tied to a convention this repo enforces or a concrete design consequence (a specific break, allocation, ambiguity, pit-of-failure). If you can't articulate a reason a maintainer would accept, **drop the recommendation** — a made-up rationale is worse than none. And **check each recommendation against the proposal's own stated goals**: never suggest something that defeats the author's explicit purpose (e.g. don't propose auto-generated/randomized values when the goal is stable, shareable output); if a change trades against a stated goal, name the tradeoff in the Why.

## Verdict

One of: **Looks good as proposed** · **Changes recommended** · **Recommend not adding** (with the need rationale). "Approved with changes" is the most common real outcome — small shape fixes on an otherwise welcome API.

### When to recommend NOT adding

Lead with the verdict and the reason. Common, real decline patterns in this repo:
- **Changing a shipped default or convention** is a breaking change — decline and tell the requester to configure/define their own (e.g. custom API conventions) instead.
- **No demonstrated demand**, or "generic extensibility someone might want" — decline/defer; point to an existing mechanism or workaround.
- **Achievable as a user/community extension method** without new framework surface — recommend that, don't add it to the framework.
- **Encourages an anti-pattern**, mixes separate concerns, or pollutes `HttpContext`/`WebApplication` IntelliSense — decline with the documented alternative.

Even when declining, still emit the need assessment (it carries the rationale) and show the proposed type in the diff as all `-` (or "no API added").

## The rules reviewers apply most (highlights)

- **Justify the surface.** Don't add API without demonstrated real-world need; generic extensibility alone isn't enough; don't change an established idiom for an uncommon case; never add APIs that encourage anti-patterns — point to the documented alternative instead. Watch for **intellisense pollution** (don't pile convenience members onto `HttpContext`/`WebApplication`) and keep **separate concerns separate** (e.g. sessions vs auth, declarative vs imperative options).
- **Optimize for the common scenario.** Picture how the API is really used, in which environments, and **where its inputs come from** (real data has a provenance — cloud storage, a DB, a file, an upload — that usually supplies natural keys/metadata). Weight the design to the dominant case. A default that's right for the common case is good even if a rare case must configure it — don't change a sensible default or complicate the common path to serve an edge case; make the edge case opt-in. A "required" input the caller already has from the data source isn't real friction — make it easy to pass, don't remove it or synthesize it expensively.
- **Naming.** Avoid implementation words (`Minimal`, `Action`); name the **mechanism**, not the intent (`ServeMultithreadingHeaders` not `EnableMultithreading`); `Create*` for static factories, `Try*` for optional lookups (nullable return), `Set*` for replace-semantics; include the scope/receiver in extension-method names (`UseKestrelHttpsConfiguration`); drop redundant qualifiers.
- **Async.** New async public methods return `Task`/`ValueTask` with a `CancellationToken cancellationToken = default` **as the last parameter** and flow it through; prefer `Task` over `ValueTask` for framework callback delegates unless a hot path justifies it; suffix async methods `Async`.
- **Types.** `IEnumerable<T>` for read-only input sequences; `IReadOnlyList<T>`/`ICollection<T>` for returns unless mutation/indexing is needed; replace anonymous tuples and `bool`+`out` with named `readonly struct`s; nullable to distinguish *unset* from *set*, non-nullable + fail-fast for required services.
- **Extensibility.** **Seal by default** (concrete, metadata, options, DTO types) — unseal later if a real scenario appears; marker interfaces are fine when presence alone conveys meaning; use generic `TBuilder` on `IEndpointConventionBuilder` extensions so they chain through `MapGroup`.
- **Language features (negative space).** **Don't ship public `record`/`record struct`** — they bake synthesized equality/`with`/`Deconstruct`/copy-ctor into the binary + ref-assembly contract that can't be evolved; use a hand-authored `class`/`struct` (records are fine `internal` / in tools / templates). Conversely, **prefer `required`/`init`-only properties over piling on constructor overloads** for mandatory/immutable inputs — that's the adopted idiom, so don't flag it (but keep options/builder props `get; set;`). `static abstract`/default-interface-methods are allowed **with a concrete justification** (generic-constrained contracts; non-breaking interface evolution). **Low usage ≠ avoidance** — recent features are scarce by recency, not policy.
- **Compatibility.** Prefer a new overload over changing an existing signature; don't obsolete a working API just because a better one landed; breaking default changes need a major version + an escape hatch; consider an analyzer instead of a breaking interface change.
- **Consistency & placement.** Mirror sibling APIs (`IResult`/`IEndpointMetadataProvider`, existing option/metric names); put niche types in a feature sub-namespace, infrastructure in `*.Infrastructure`, concrete impls beside their interface; pick the type suffix that matches the role (`Options`/`Context`/`Feature`/`Provider`/`Result`/`Factory`/`Builder`/`Metadata`/`Defaults`).
- **Method families have a fixed contract:** `Add*` registers services → `IServiceCollection`/feature builder, in `Microsoft.Extensions.DependencyInjection`; `Use*`/`Map*` add middleware/endpoints → `IApplicationBuilder`/endpoint convention builder, in `Microsoft.AspNetCore.Builder`; `With*` attaches endpoint metadata → returns the same builder. Placement holds **regardless of the feature's namespace**; wrong return type/namespace/job is a finding.
- **Type & assembly placement.** Durable interface/abstract contract → the area's `*.Abstractions` (or a primitive sink); judge an assembly's role by **dependency position, not name** (abstractions are sinks). Conceptual base → Abstractions; reusable algorithmic base → `*.Core`; `Default*`/sealed impls → implementation/provider assembly. A namespace≠assembly mismatch is **not** a smell. A feature carrying a **third-party dependency ships as standalone NuGet, not the shared framework**.
- **Type relationships.** Framework default impl is `Default<X> : X`; provider options/handlers derive the narrowest family base; a minimal-API result implements `IResult` + truthful granular markers + `IEndpointMetadataProvider`; behavioral attributes implement the framework interface carrying the behavior; **reuse an existing extension-point interface before inventing one**.

For the type-placement and relationship details see [conventions.md](references/conventions.md#type--assembly-placement).

## Reviewing checklist

Need assessed (commonality + audience) · diff is against the real current surface · every member checked for naming/async/nullability/return-type/overload/sealing/namespace/compat · **type placement (assembly role + namespace + packaging) and type relationships (base/interface, `Default<X>`, companions, reused extension points) checked** · **every recommended change has a grounded Why and none defeats a stated goal of the proposal** · verdict matches the body · output follows [output-format.md](references/output-format.md).
