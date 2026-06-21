# API review process & framing

## The lifecycle

Public API changes in dotnet/aspnetcore go through API review (`docs/APIReviewProcess.md`):

`api-suggestion` (early idea) → owner marks **`api-ready-for-review`** when the proposal is in good shape (and notifies `@dotnet/aspnet-api-review`) → reviewed in the weekly API review meeting → **`api-approved`** or **`api-needs-work`**. If the design changes during implementation, the review restarts. The proposal template (`30_api_proposal.md`) asks for: Background & Motivation, Proposed API, Usage Examples, Alternative Designs, Risks.

The written conventions doc (`docs/APIReviewPrinciples.md`) is a stub; the actual conventions are in the decision threads, captured in [conventions.md](conventions.md). The repo defers to the [.NET Framework Design Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/) for anything not ASP.NET-Core-specific.

## Ref-assembly format

Proposals are reviewed as **reference-assembly signatures** — public types and members only, no method bodies, no doc comments. This compact form makes shape patterns easy to spot. When you review, work in this format, and produce your recommended shape the same way.

There is **no checked-in ref source** in this repo (ref assemblies are generated at build via `Microsoft.AspNetCore.App.Ref`). To diff your recommendation against the *current* API, locate the relevant public type(s) under `src/**/src/` and reconstruct their present public signature.

## Should this API exist?

API review's first question is **whether to add the API at all**, not just its shape. Before you can answer it, **model how the API is really used** — this grounds the commonality judgment in reality instead of guesswork:

- **Who calls it** — ordinary app code, or framework/library authors building on top?
- **What environments it runs in** — e.g. static SSR vs interactive rendering, Blazor Server vs WebAssembly, single-process vs scaled-out/load-balanced, AOT/trimmed vs JIT, sync vs async hosts. The same API can be common in one environment and irrelevant in another.
- **Which usage patterns dominate** — the typical shape of consuming code, and how often the proposed scenario actually occurs versus the rare variants. *Example:* a page usually has **one** main data grid; **many** grids on one page is a real but uncommon case. So an empty/default query-prefix is fine for the common single-grid case, and the multi-grid case is served by an opt-in prefix — you wouldn't change the default or complicate the common path to handle the rare one.
- **The nature and provenance of the inputs** — where the values the API consumes actually come from in real apps, and what is naturally available at that call site. Data rarely materializes from nowhere: bytes come from Azure Blob Storage, a database BLOB, a file, or a user upload — sources that almost always already carry a stable identifier (blob name/ETag, primary key, file path, content-disposition name) and metadata (length, content type). This reframes whether a "required" parameter is real friction: a required `cacheKey` looks burdensome in the abstract, but if the caller already holds a natural key from the source, the right move is to **make it easy to pass that value**, not to remove the parameter or synthesize one expensively (e.g. hashing the whole stream, which defeats streaming). Only the genuinely rare case — bytes synthesized in memory — lacks a natural identifier; serve that, don't optimize for it.

Then frame the two axes:

- **Commonality** — how often will this be used?
  - *happy-path* — most apps in this area need it → strong case to add.
  - *extensibility* — only some apps/libraries need to plug in → add only with a demonstrated scenario.
  - *edge-case* — rare/niche → lean toward not adding; document a workaround.
- **Target audience** (precedence high → low):
  1. **end-app-developer** — the API serves ordinary app code. Highest priority.
  2. **library-author** — extensibility for frameworks/libraries built on ASP.NET Core. Justified only with a concrete consumer scenario.
  3. **unjustified / generic extensibility** — "someone might want to…" with no real scenario. Decline.

**Optimize for the common case.** Pick defaults and the primary API shape for the dominant scenario; make rarer scenarios reachable by opt-in configuration rather than by penalizing or reshaping the common path. Reviewers repeatedly choose this — "most of the time developers do not need or want to do this" (#44320), "uncommon enough that I don't think providing a first class option is necessary" (#48421). Recurring decline reasons in this repo: no demonstrated demand, encourages an anti-pattern, intellisense pollution, mixes separate concerns, or duplicates an existing general mechanism (see [conventions.md](conventions.md#should-this-exist-need--audience)).

A "recommend not adding" verdict is a legitimate, common review outcome — say so clearly and explain the alternative.
