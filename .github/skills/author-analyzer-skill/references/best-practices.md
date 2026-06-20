# Roslyn analyzer & code-fix best practices

Each rule below names the enforcing `RSxxxx` meta-analyzer where one exists, so you can look it up. `EnforceExtendedAnalyzerRules=true` in the Framework analyzer projects turns many of these on as build errors — treat them as hard requirements, not suggestions.

## Table of contents
- [Performance](#performance)
- [Concurrency & state](#concurrency--state)
- [Correctness](#correctness)
- [Diagnostic design](#diagnostic-design)
- [Code fixes](#code-fixes)
- [Anti-patterns (do NOT do)](#anti-patterns-do-not-do)

## Performance

An analyzer's callbacks run on every node/symbol/operation of their kind, in every file, live in the IDE. Allocation and redundant work here are felt as editor lag.

- **Cache well-known symbols once per compilation.** Resolve them inside `RegisterCompilationStartAction` and close over the result; never resolve per node/symbol. In this repo, use `WellKnownTypes.GetOrCreate(compilation)`.
- **Prefer `IOperation`/symbol analysis over syntax** for anything semantic. It is faster, allocates less, and is language-agnostic. Use `RegisterSyntaxNodeAction` only for purely structural checks.
- **Bail out cheapest-first.** Check language version, then a quick syntactic gate, then parse errors, then expensive semantic work — `return` at the first failing gate.
- **No allocations in hot callbacks.** Avoid `new List<>()`, LINQ, and `DescendantNodes()` walks inside per-node/per-operation actions. Scope walks with `RegisterCodeBlockStartAction` / `RegisterOperationBlockStartAction`. When you must accumulate, reuse buffers and avoid intermediate LINQ; Roslyn's pooled collections (e.g. `ArrayBuilder<T>.GetInstance(...)`) are an option where they are accessible.
- **`node.IsKind(SyntaxKind.X)`** rather than `node.Kind() == SyntaxKind.X` (RS1034).
- **Never call `Compilation.GetSemanticModel()` inside a callback** (RS1030) — it bypasses the host's semantic-model cache and forces a full re-bind, causing visible IDE hangs. Use the model already on the action context.

## Concurrency & state

- **Call `context.EnableConcurrentExecution()`** in `Initialize` (RS1026) so the host can parallelize.
- **Call `context.ConfigureGeneratedCodeAnalysis(...)`** (RS1025). Use `GeneratedCodeAnalysisFlags.None` for most rules. *Caveat:* Razor/Blazor markup compiles to generated C#, so an analyzer meant to inspect output of `.razor`/`.cshtml` must instead pass `GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics`, or it will silently skip that code.
- **Analyzers must be stateless across compilations.** Never store `Compilation`, `ISymbol`, or `IOperation` (or anything holding them) in instance fields (RS1008) — instances are reused across compilations and concurrent callbacks. Flow state through start-action closures.
- With concurrency enabled, any state shared between your own callbacks must be thread-safe (`ConcurrentDictionary`, immutable collections). **Don't depend on action ordering** except the start→inner→end nesting guarantee, and **don't rely on compilation-end actions running** in the IDE.

## Correctness

- **`SupportedDiagnostics` must list every descriptor you ever report** (RS1005). Expose them as a cached `ImmutableArray`.
- **Compare symbols with `SymbolEqualityComparer.Default`** (RS1024), never `==`/`!=` — symbol identity isn't stable across compilations.
- **Don't use `Microsoft.CodeAnalysis.Workspaces` types** (`Document`, `Solution`, `Workspace`) inside a `DiagnosticAnalyzer` (RS1022) — that assembly isn't loaded during `dotnet build`. Those types belong in code fixes only. *(Enforced in the Framework set via `EnforceExtendedAnalyzerRules`; the Components and MVC API analyzer projects deliberately reference Workspaces for VS compatibility — follow the no-Workspaces rule for new analyzers regardless.)*
- **Localizable text + help link.** Title/message/description are `LocalizableResourceString` (RS1007); provide a non-null help link (RS1015 — derived from the ID in this repo). Title has no trailing period (RS1031); message ends with one (RS1032).
- Diagnostic IDs are compile-time constants (RS1017), unique (RS1019), and must not use reserved prefixes like `CS`/`CA`/`IDE` (RS1029) — use the area prefix (`ASP`/`MVC`/`API`/`BL`).
- Don't register a start action with no inner action (RS1012/RS1013).

## Diagnostic design

- **Severity** — see [process.md](process.md#severity). Default-on `Warning` is the common choice; reserve `Error` for "always wrong / will break"; use `Info`/`Hidden` for suggestions and fix-only squiggles.
- **Pick the right registration:** symbol declared → `RegisterSymbolAction`; what executable code does → `RegisterOperationAction`; pure shape → `RegisterSyntaxNodeAction`; cross-file → compilation start/end; whole body with shared state → operation-block start + inner + end.
- **Report on the smallest meaningful span** — the keyword/token/operator at fault, not the whole statement.
- **Pass pre-computed data to the fixer via `diagnostic.Properties`** (an `ImmutableDictionary<string,string?>`) so the fix doesn't redo semantic analysis.
- **Minimize false positives.** Bail when the target node has parse errors (`GetDiagnostics().Any(d => d.Severity == Error)`) or contains interleaved `#directives`. If a case is ambiguous, don't report — a missed diagnostic is far better than a wrong one.
- Use `WellKnownDiagnosticTags.Unnecessary` for fade-out (grey) diagnostics; add the `CompilationEnd` tag to descriptors only reported from compilation-end actions (RS1037).
- **Silencing a *compiler/other* diagnostic** (rather than reporting a new one) is a different tool: a `DiagnosticSuppressor`. Reach for it when a built-in warning is a known false positive in an ASP.NET Core pattern (e.g. async-without-await in a minimal API handler) — not to hide your own analyzer's output.

## Code fixes

- `[ExportCodeFixProvider(LanguageNames.CSharp), Shared]`; `FixableDiagnosticIds` lists exactly the IDs handled.
- **Every `CodeAction` needs a non-null `equivalenceKey`** (RS1010). Choose the key for the FixAll scope you want: the rule ID batches all occurrences; a per-occurrence key batches only identical ones.
- **Override `GetFixAllProvider()`** (RS1016). `WellKnownFixAllProviders.BatchFixer` works when fixes are local, non-overlapping, and apply-changes only; otherwise write a **singleton** custom `FixAllProvider`.
- Keep fixes minimal and syntactically correct; preserve/transfer trivia and add `Formatter.Annotation` for reformatting. Propagate `CancellationToken`; `await ... .ConfigureAwait(false)`.

## Anti-patterns (do NOT do)

| ❌ Don't | ✅ Do | Enforced by |
|---------|------|-------------|
| Store `Compilation`/`ISymbol`/`IOperation` in a field | Close over them in a start action | RS1008 |
| `Compilation.GetSemanticModel()` in a callback | Use the context's semantic model | RS1030 |
| Reference `Document`/`Solution`/`Workspace` in an analyzer | Keep those in the code fix | RS1022 |
| Compare symbols with `==` | `SymbolEqualityComparer.Default.Equals` | RS1024 |
| Allocate / LINQ / `DescendantNodes()` in per-node callbacks | Pooled builders; scope to blocks | — |
| Plain-string title/message | `LocalizableResourceString` | RS1007 |
| `CodeAction.Create` without an equivalence key | Pass a stable `equivalenceKey` | RS1010 |
| Report on a whole statement/declaration | Report on the smallest faulting span | — |
| Report when the target has parse errors or interleaved directives | Bail out — avoid false positives | — |
| Reuse a retired diagnostic ID | Allocate the next free ID | RS1019 |
